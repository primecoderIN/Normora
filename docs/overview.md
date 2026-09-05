# Normora - Project Overview

## What is Normora?
Normora is an enterprise-grade full-stack web application. It is designed to be highly scalable, thoroughly secure, and remarkably easy to maintain for growing engineering teams.

## Key Features
- **Strict Scalable Architecture**: Utilizes a highly disciplined modular monolith approach on the backend. True boundary enforcement allows for easy transitioning to microservices as the product scales.
- **Modern Frontend**: Built with Angular, providing a highly responsive, strongly-typed, and robust single-page application experience.
- **Identity & Security First**: Integrated deeply with Keycloak for industry-standard identity, authentication, and access management.
- **Reliable Storage Ecosystem**: Uses PostgreSQL for relational data across isolated schema boundaries, and MinIO for highly scalable S3-compatible object storage.
- **Document Lifecycle Visibility**: Uploaded documents expose a clear `Uploaded`, `Processing`, `Ready`, or `Failed` state. Background extraction and indexing will advance documents beyond the initial upload state in the next ingestion slice.

## Target Audience
This platform is built to accommodate complex enterprise workloads, providing robust multi-tenant data isolation, stringent user management, and seamless document handling out of the box.
