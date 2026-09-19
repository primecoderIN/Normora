# How Normora Works: Document Sharing, Grouping, and Secure AI Retrieval

Welcome to Normora! This guide explains the complete end-to-end flow of how document sharing, grouping, and retrieval works in the application.

The entire system is designed around **Secure Conversational RAG (Retrieval-Augmented Generation)**. The primary goal is to ensure that when an employee asks the AI a question, the AI *only* retrieves and uses documents that the employee is explicitly authorized to see.

---

## Phase 1: Configuration (How an Admin sets it up)

The flow begins with an Administrator (Employer) structuring the knowledge base:

1. **Create Departments**: The Admin creates logical folders or subdivisions of data. 
   *(Examples: "Human Resources", "Engineering", "Executive Team")*
2. **Upload Documents**: The Admin uploads company files (PDFs, docs, text). During upload, the document is securely tagged to one or more Departments. 
   *(Example: "Q3 Financial Projections.pdf" is uploaded and assigned ONLY to the "Executive Team" department).*
3. **Create User Groups**: The Admin creates logical groups for employees and maps these groups to Departments. 
   *(Example: Admin creates a "Managers" User Group and links it to the "Human Resources" and "Executive Team" departments).*
4. **Assign Users**: Finally, the Admin goes to the **Users/Team** page and adds specific employees to those User Groups.

---

## Phase 2: The Data Processing Pipeline (Under the hood)

When a document is uploaded, Normora doesn't just save the file. It prepares it for AI search:

- **Chunking**: The document is split into smaller, meaningful paragraphs (chunks).
- **Embedding**: Each chunk is passed through an embedding model to convert the text into mathematical vectors.
- **Storage**: The vectors are stored in PostgreSQL using the `pgvector` extension, securely linked to the Tenant ID and the Department ID.

---

## Phase 3: The Conversation Flow (How an Employee uses it)

Now, an employee logs into the chat interface and asks a question:

> *"What are the Q3 financial projections?"*

Here is exactly what happens in milliseconds:

1. **Authorization Resolution**: 
   The backend identifies the employee. It calculates every department they have access to. If this employee is in the "Managers" User Group, the system knows they have access to the "Human Resources" and "Executive Team" departments.

2. **Context Understanding**:
   The system looks at the conversation history to see if the question requires context. *(e.g., if they asked "What about Q4?" it would use a small LLM to rewrite the query into a standalone question).*

3. **Secure Hybrid Retrieval**:
   The system runs a vector similarity search against the `pgvector` database to find text that matches "Q3 financial projections". 
   **Crucially**, the database query includes a strict filter: 
   `WHERE Document.DepartmentId IN (User's Authorized Departments)`
   - If a normal Developer asks this question, the vector search will physically ignore the Executive Team documents and return nothing.
   - If the Manager asks, it retrieves the authorized chunks from "Q3 Financial Projections.pdf".

4. **Reranking**:
   The retrieved candidate text chunks are scored and reranked to ensure the absolute most relevant paragraphs are pushed to the top.

5. **LLM Generation**:
   The system builds a prompt for the main LLM that looks like this:
   *“You are a helpful assistant. Answer the user's question using ONLY the provided authoritative documents. If the answer isn't in the documents, say you don't know.”*
   It injects the secure text chunks below this instruction.

6. **Streaming & Citations**:
   The LLM generates the answer and streams it back to the user's screen in real-time. Alongside the answer, the backend returns **Message Citations**—a list of the exact documents and chunks that were used to generate the answer, which the UI renders as clickable references.

---

## Summary

By separating **Knowledge Memory** (Documents) from **Conversation Memory** (Chats), and using **User Groups** as the bridge, Normora guarantees that the AI acts as a highly secure, tenant-aware oracle. Employees can converse naturally, and administrators only have to manage role-based checkboxes to keep data strictly confidential.
