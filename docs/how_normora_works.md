# How Normora Works: Document Ingestion, Departmental RBAC & Grounded Conversational AI

Welcome to **Normora**! This guide provides an end-to-end visual walkthrough of how document ingestion, organizational scoping, and secure conversational AI operate together.

The foundational design principle of Normora is **Zero-Trust Conversational RAG**: when an employee converses with the AI, the system guarantees that the AI *only* retrieves and reasons over documents that the employee's assigned departments explicitly authorize them to see.

---

## 1. High-Level Lifecycle Overview

```mermaid
flowchart TD
    subgraph Phase1 ["Phase 1: Admin Governance & Upload"]
        A1["1. Admin creates Departments<br/>(e.g., HR, Engineering, Executive)"]
        A2["2. Admin creates User Groups &<br/>maps them to Departments"]
        A3["3. Admin assigns Employees to User Groups"]
        A4["4. Admin uploads Document & tags<br/>to authorized Departments"]
    end

    subgraph Phase2 ["Phase 2: Asynchronous Processing Pipeline"]
        B1["MinIO Object Storage"]
        B2["Hangfire Ingestion Worker"]
        B3["Apache Tika Text Extraction"]
        B4["Token-Bounded Text Chunking"]
        B5["Gemini Embedding Generation<br/>(768 dimensions)"]
        B6["pgvector HNSW Vector Store<br/>(schema: documents)"]
        B7["SignalR Real-Time WebSocket<br/>(Status Broadcast)"]
    end

    subgraph Phase3 ["Phase 3: Conversational AI & Grounded Answering"]
        C1["Employee asks question in Chat"]
        C2["BFF Session & Department RBAC Resolution"]
        C3["Query Rewriter (Context Awareness)"]
        C4["Hybrid Retrieval (pgvector + Keyword FTS via RRF)"]
        C5["Department Security Filter (WHERE DeptId IN UserDepts)"]
        C6["Autonomous Agent Tools (Policy Limit, HR Escalation)"]
        C7["Server-Sent Events (SSE) Grounded Stream + Citations"]
    end

    Phase1 --> Phase2
    Phase2 --> Phase3
```

---

## 2. Phase 1: Configuration & Departmental Scoping

The administrator structures the organizational knowledge graph:

1. **Departments**: Logical subdivisions of company data (e.g., *"Human Resources"*, *"Engineering"*, *"Executive Leadership"*).
2. **Document Ingestion**: The admin uploads company documents (PDF, DOCX, TXT). Each document is assigned to one or more departments (or marked *Company-Wide*).
   > *Example*: `Q3_Financial_Projections.pdf` is assigned exclusively to the **Executive Leadership** department.
3. **User Groups**: Logical employee cohorts mapped to one or more departments.
   > *Example*: A *"Senior Managers"* user group is granted access to both **Human Resources** and **Executive Leadership**.
4. **User Assignment**: Employees are added to User Groups, automatically inheriting access to corresponding departmental knowledge partitions.

---

## 3. Phase 2: Ingestion & Vector Indexing Pipeline

When an employer uploads a document, Normora offloads compute-heavy processing to an asynchronous background worker pipeline:

```mermaid
sequenceDiagram
    autonumber
    actor Employer as Employer (Admin)
    participant API as Normora.Api
    participant MinIO as MinIO (S3 Storage)
    participant DB as PostgreSQL (documents schema)
    participant Outbox as Outbox Dispatcher
    participant HF as Hangfire Worker (Normora.Worker)
    participant Tika as Apache Tika
    participant Gemini as Google Gemini API
    participant Hub as SignalR (DocumentHub)

    Employer->>API: 1. POST /api/documents (Upload File)
    API->>MinIO: 2. Store original binary (Encrypted S3 bucket)
    API->>DB: 3. Commit Document, Version & Outbox Record (Status: Uploaded)
    API->>Outbox: 4. Signal processing wakeup
    API-->>Employer: 5. 202 Accepted (Document ID)
    Outbox->>HF: 6. Enqueue DocumentProcessingJob

    Note over HF: Asynchronous Processing in Worker Process
    HF->>DB: 7. Update Status -> Processing
    HF->>Hub: 8. Broadcast WebSocket: Processing
    HF->>MinIO: 9. Download raw file bytes
    HF->>Tika: 10. Extract clean plain text
    Tika-->>HF: 11. Normalized text
    HF->>HF: 12. Normalize & split into bounded chunks (sliding window)
    HF->>Gemini: 13. Generate 768-dim embeddings in batches
    Gemini-->>HF: 14. Embedding vectors
    HF->>DB: 15. Bulk insert DocumentChunks with pgvector HNSW index
    HF->>DB: 16. Update Status -> Ready
    HF->>Hub: 17. Broadcast WebSocket: Ready
```

1. **Transactional Outbox Decoupling**: Original files are saved to MinIO S3 object storage; file metadata, version, and a `DocumentProcessingOutboxMessage` are committed atomically within the same database transaction, ensuring no work is lost even if downstream background dispatch experiences transient faults. The API immediately returns `202 Accepted`.
2. **Text Normalization**: Apache Tika extracts structured text across file formats, handling headers, footnotes, and tables.
3. **Bounded Chunking**: Cleaned text is segmented into optimal token chunks with sliding-window overlaps to preserve context.
4. **HNSW Vector Storage**: 768-dimensional embeddings are indexed in PostgreSQL using pgvector HNSW (`vector_cosine_ops`, `m = 16`, `ef_construction = 64`), enabling sub-millisecond approximate nearest neighbor lookups under enterprise volume.
5. **Real-Time Visibility**: Hangfire pushes lifecycle updates over SignalR (`Uploaded` ➔ `Processing` ➔ `Ready` / `Failed`), updating the employer's dashboard live without browser polling.

---

## 4. Phase 3: Conversational RAG & Departmental Isolation

When an employee asks a question in the chat interface:

> *"What are the Q3 financial projections and travel meal caps?"*

The system executes the following secure pipeline:

```mermaid
flowchart TD
    User([Employee]) --> Chat["Chat Interface (Angular SPA)"]
    Chat -->|POST /api/conversations/{id}/messages/stream| API["Conversations API"]

    subgraph AuthLayer ["1. Authorization & Context Resolution"]
        API --> Auth["BFF Session Check & Cached Tenant Resolution"]
        Auth --> ResolveDepts["Calculate User's Effective Department IDs<br/>(e.g., Engineering only)"]
        API --> Rewriter{"Context Required?"}
        Rewriter -->|Follow-up Question| RewriteLLM["Query Rewriter LLM<br/>Produces Standalone Query"]
        Rewriter -->|Standalone Question| RawQuery["Original User Query"]
    end

    subgraph RetrievalLayer ["2. Department-Filtered Hybrid Retrieval"]
        RawQuery & RewriteLLM --> Hybrid["Hybrid Retrieval Service"]
        Hybrid --> Vec["pgvector Cosine Similarity<br/>(WHERE DeptId IN UserDepts)"]
        Hybrid --> FTS["Full-Text Keyword Search<br/>(WHERE DeptId IN UserDepts)"]
        Vec & FTS --> RRF["Reciprocal Rank Fusion (RRF)<br/>Score = SUM(1 / (60 + Rank))"]
        RRF --> Rerank["Top Authoritative Chunks Pool"]
    end

    subgraph GenerationLayer ["3. Autonomous Agent Tools & Streaming"]
        Rerank --> Context["Assemble Grounded Prompt<br/>(System Instructions + Chunks)"]
        Context --> Gemini["Gemini Flash LLM (Function Calling Loop)"]
        Gemini -->|Autonomous Decision| Tools{"Tool Call?"}
        Tools -->|calculate_policy_limit| ToolCalc["Compute Meal Allowance"]
        Tools -->|escalate_hr_inquiry| ToolEsc["Send HR Admin Escalation Email"]
        ToolCalc & ToolEsc --> Gemini
        Tools -->|Synthesize Final Answer| Stream["SSE Token Stream + Citations"]
    end

    Stream -->|Real-time Tokens & Clickable Citations| Chat
```

### Critical Security Boundaries
1. **Physical Retrieval Partitioning**: The vector similarity query applies a hard database filter:
   ```sql
   WHERE ("TenantId" = @tenantId) 
     AND ("DepartmentId" IS NULL OR "DepartmentId" = ANY(@authorizedDepartmentIds))
   ```
   If a junior engineer asks about Executive Financials, the database query physically ignores executive documents, guaranteeing that unauthorized chunks are never read into memory or passed to the LLM.
2. **Autonomous Tool Calling**: When the user's inquiry requires policy computations (e.g. travel limits) or human intervention (e.g. escalating an unanswered question), Gemini autonomously triggers registered agent tools (`calculate_policy_limit`, `escalate_hr_inquiry`) inside the user's tenant context.
3. **Transparent Citations**: Every generated answer streams alongside verified **Message Citations** (document title, chunk ID, relevance rank, and preview text), allowing employees to audit and verify source material.
