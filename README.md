# 🚀 Normora

<div align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Angular-DD0031?logo=angular&logoColor=white" alt="Angular" />
  <img src="https://img.shields.io/badge/PostgreSQL-336791?logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Keycloak-0096D6?logo=keycloak&logoColor=white" alt="Keycloak" />
  <img src="https://img.shields.io/badge/MinIO-C72C48?logo=minio&logoColor=white" alt="MinIO" />
  <img src="https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white" alt="Docker" />
</div>
<br>

**Normora** is an enterprise-grade full-stack web application designed for high scalability, stringent security, and an exceptional developer experience.

## 🌟 Why Normora?
Normora is architected from the ground up to deliver a resilient and performant modern web experience:

- **Strict Modular Monolith Backend**: Built on **.NET 10**, the backend gracefully enforces strict domain boundaries. Independent modules (such as Tenants, Documents, Auth) own their isolated persistence layers and infrastructure concerns. This allows for seamless vertical scaling today and effortless extraction into microservices in the future.
- **Modern SPA Frontend**: Powered by **Angular**, providing a lightning-fast, reactive, and strictly-typed user experience.
- **Enterprise-Ready Infrastructure**:
  - **Identity & Access Management**: Fully integrated with **Keycloak** via OAuth2/OpenID Connect to handle secure authentication and robust role-based access control.
  - **Object Storage**: Leverages **MinIO** for S3-compatible, highly performant, and horizontally scalable file storage.
  - **Relational Data**: Powered by **PostgreSQL** for reliable ACID-compliant transactions across isolated module schemas.
- **Containerization & DevOps**: The entire application and its complex dependencies are thoroughly Dockerized. It highlights a focus on developer experience (DX)—a single script provisions the database, IAM server, object storage, API, and the Web Client from scratch.

## 📚 Documentation
Dive deeper into the project by checking out our dedicated documentation suite:

- 📖 [**Project Overview**](./docs/overview.md) - High-level summary of features and use cases.
- 🏗️ [**Architecture Details**](./docs/architecture.md) - Deep dive into the Modular Monolith, technical stack, and system design decisions.
- 🚀 [**Getting Started**](./docs/getting_started.md) - A step-by-step guide to spinning up the environment locally using Docker.

## ⚡ Quick Start

Starting the whole stack locally is incredibly simple:

```powershell
.\start.ps1
```
*(This script will automatically generate a `.env` file with defaults, pull necessary images, and build the .NET API & Angular client).*

To gracefully stop the environment without destroying your data volumes:
```powershell
.\stop.ps1
```

## 🌐 Access Points

Once the Docker containers are running, you can access the ecosystem at:
- **Client Application (Angular)**: `http://localhost:4200`
- **Backend API (.NET 10)**: `http://localhost:5000`
- **API Documentation & Testing (Scalar)**: `http://localhost:5000/scalar/v1`
- **Keycloak Admin Console**: `http://localhost:8080`
- **MinIO Console**: `http://localhost:9001`
- **Mailpit (Email Testing UI)**: `http://localhost:8025`
