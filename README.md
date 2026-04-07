# 📄 Serverless Document Processor (Azure Functions)

A **serverless document processing system** built using Azure Functions and Azure Blob Storage.
It allows users to upload documents, processes them asynchronously, sends email notifications using **SendGrid**, and automatically deletes expired files.

---

# 🚀 Features

* 📤 Upload documents via HTTP API
* ☁️ Store files in Azure Blob Storage
* ⚙️ Automatic processing using Blob Trigger
* 📧 Email notifications via SendGrid
* 🗑️ Automatic cleanup of expired files (30 days)
* ⏱️ Scheduled background cleanup job

---

# 🧱 Architecture Overview

```id="arch1"
Client → HTTP Trigger → Blob Storage → Blob Trigger → Send Email
                                         ↓
                                  Add Expiry Metadata
                                         ↓
                                Timer Trigger (Cleanup)
```

---

# 🔄 Application Flow

## 1️⃣ Upload Flow (HTTP Trigger)

Function: `UploadDocument`

### Steps:

1. Client sends a **POST request**
2. Required headers:

   * `UploaderEmail`
   * `FileName`
3. File is uploaded to Azure Blob Storage (`uploads` container)
4. Metadata is added:

   * `UploaderEmail`
   * `UploadedOn`
5. Returns `202 Accepted`

---

## 2️⃣ Processing Flow (Blob Trigger)

Function: `ProcessDocument`

### Steps:

1. Triggered automatically when a file is uploaded
2. Reads metadata:

   * `UploaderEmail`
3. Simulates document processing
4. Sends email:

   * ✅ "Your document has been processed"
5. Adds metadata:

   * `ExpiryDate` (30 days from upload)

---

## 3️⃣ Cleanup Flow (Timer Trigger)

Function: `CleanupOldFiles`

### Schedule:

* Runs **daily at midnight**

### Steps:

1. Scans all blobs in `uploads` container
2. Checks `ExpiryDate`
3. If expired:

   * Deletes the file
   * Sends email:

     * ❌ "Your document has been deleted"

---

# ☁️ Azure Services Used

* Azure Functions
* Azure Blob Storage
* SendGrid Email API

---

# ⚙️ Environment Variables

Configure these in:

* `local.settings.json` (local development)
* Azure Portal → Function App → Configuration

---

## 🗄️ Storage Configuration

```json
StorageConnectionString=your_azure_storage_connection_string
```

Used for:

* Blob upload
* Blob trigger
* Cleanup job

---

## 📧 SendGrid Configuration

```json
SENDGRID_API_KEY=your_sendgrid_api_key
SENDGRID_FROM_EMAIL=verified_sender@email.com
```

### ⚠️ Important:

* You must **verify your sender email/domain** in SendGrid
* Otherwise, emails will fail

---

# 🧪 Local Development Setup

## 1. Prerequisites

* .NET SDK
* Azure Functions Core Tools

---

## 2. local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "StorageConnectionString": "your_connection_string",
    "SENDGRID_API_KEY": "your_api_key",
    "SENDGRID_FROM_EMAIL": "your_verified_email"
  }
}
```

---

## 3. Run the project

```bash
func start
```

---

# 📬 API Usage

## Upload Document

### Endpoint:

```
POST /api/UploadDocument
```

### Headers:

```
UploaderEmail: user@example.com
FileName: test.pdf
```

### Body:

* Raw file stream

---

# 🧠 Blob Metadata Structure

| Key           | Description       |
| ------------- | ----------------- |
| UploaderEmail | Email of uploader |
| UploadedOn    | Upload timestamp  |
| ExpiryDate    | Auto-delete date  |

---

# 🔐 Security Notes

* Never commit API keys to GitHub
* Use environment variables or Azure Key Vault
* Use Function-level authorization (`AuthorizationLevel.Function`)

---

# 🧪 Testing

## ✅ Local Testing

* Run function locally
* Use Postman or curl

## ✅ SendGrid Sandbox Mode (Optional)

To test without sending real emails:

```csharp
msg.MailSettings = new MailSettings
{
    SandboxMode = new SandboxMode
    {
        Enable = true
    }
};
```

---

# 📁 Project Structure

```
Serverless_Doc_Processor/
│
├── DocProcess.cs              # Azure Functions (HTTP, Blob, Timer)
│
├── Services/
│   └── SendGridEmailService.cs
│
├── Interfaces/
│   └── IEmailService.cs
```

---

# 🔮 Future Improvements

* Add real document processing (OCR / AI)
* Add authentication (JWT / Azure AD)
* Validate file types and size
* Add retry policies for email sending
* Integrate Azure Queue for better scalability

---

# 🏁 Summary

This project demonstrates a complete **event-driven serverless pipeline**:

**Upload → Store → Process → Notify → Expire → Cleanup**

* Fully scalable
* Minimal infrastructure
* Cloud-native design

---
