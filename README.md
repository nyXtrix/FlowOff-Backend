# FlowOff - Leave Management System

FlowOff is a robust, multi-tenant Leave Management System designed to handle complex organizational structures, dynamic leave policies, and multi-stage approval workflows.

## Project Architecture

The project follows the **Clean Architecture** (Onion Architecture) pattern, ensuring a strict separation of concerns and making the system highly testable and maintainable.

### 1. LMS.Domain (Core)
- **Entities**: Pure C# classes representing the database tables.
- **Enums**: System-wide constants (Leave Status, Approval Status, Gender, etc.).
- **Common**: Base classes like `BaseEntity`.
- *Rule: No dependencies on other projects.*

### 2. LMS.Application (Business Logic)
- **Features**: Grouped by module (Leaves, Organization, Auth). Contains Services, DTOs, and Interfaces.
- **Common**: Shared logic like Security Strategies, Cache interfaces, and Paginated results.
- **Interfaces**: Defines what the Infrastructure layer must implement (Email, Storage, etc.).

### 3. LMS.Infrastructure (Implementation)
- **Persistence**: EF Core `AppDbContext` and Migrations.
- **Services**: Concrete implementations of external services (Redis Cache, MailKit Email, Google Drive Storage).
- **BackgroundWorkers**: Periodic tasks like resetting daily invite quotas.

### 4. LMS.API (Presentation)
- **Controllers**: RESTful API endpoints.
- **Middleware**: Exception handling, Permission verification, and Multi-tenancy detection.
- **Configuration**: Program.cs and Service registrations.

---

## Tech Stack & Libraries

| Library | Purpose | Feature |
| :--- | :--- | :--- |
| **EF Core & Npgsql** | Database Provider | Postgres persistence and query optimization. |
| **StackExchange.Redis** | Caching | Distributed caching for Approval lists and User permissions. |
| **MailKit / Google API** | Email Service | Transitioned from legacy SMTP to **Google Gmail API (REST)** for higher reliability and OAuth2 security. |
| **Sylvan.Data.Csv** | CSV Processing | Extremely fast parsing for Bulk Employee Invitations. |
| **ExcelDataReader** | Excel Processing | Handling .xlsx files in the bulk onboarding module. |
| **Google.Apis.Drive** | Cloud Storage | Secure storage for uploaded bulk invitation files. |
| **BCrypt.Net** | Security | Secure password hashing for user accounts. |

---

## Email Service Transition

We recently migrated our email notification infrastructure to provide a more robust and secure experience.

### **Old Implementation: SMTP (MailKit)**
- **Method**: Used standard SMTP protocols (Ports 587/465).
- **Issues**: Frequently blocked by cloud provider firewalls, lower delivery rates, and required "App Passwords" which are less secure.

### **New Implementation: Google Gmail API (REST)**
- **Method**: Uses the **Google.Apis.Gmail.v1** library to send mail over HTTPS.
- **Benefits**:
    - **Security**: Uses **OAuth2** with Refresh Tokens instead of static passwords.
    - **Reliability**: REST API calls are less likely to be flagged as spam or blocked by firewalls compared to traditional SMTP.
    - **Performance**: Better handling of rate limits and modern email standards.

---

## Security & Permissions Architecture

FlowOff uses a highly granular, hybrid authorization model combining **Role-Based Access Control (RBAC)** with **User-Level Overrides**.

### **1. How Permissions are Assigned**
A user's total permission set is calculated as:
`Total Permissions = (Role Permissions) + (User Positive Overrides) - (User Negative Overrides)`

- **Role-Based**: Standard permissions defined at the Role level (e.g., "Employee", "Manager").
- **Granular Overrides**: The `Users` table contains a `PermissionOverridesJson` column. This allows admins to grant or revoke specific permissions for an individual user without changing their entire role.

### **2. API Authorization Enforcement**
Permissions are enforced at the API layer using a custom attribute:
```csharp
[AuthorizePermission("Leave.Approve")]
public async Task<IActionResult> ApproveLeave(...) { ... }
```
- If a user lacks the required permission string in their resolved set, the API returns a `403 Forbidden` automatically.

### **3. Permission Resolution & Caching**
To ensure high performance, permissions are managed as follows:
- **Middleware**: The `UserPermissionMiddleware` intercepts every request to resolve the user's permissions.
- **Redis Caching**: Resolved permissions are cached in **Redis** with a prefix `upr_{userExternalId}`. This prevents redundant database lookups on every API call.
- **Invalidation**: Caches are automatically purged when a user's role or overrides are updated.

### **4. Multi-Tenant Data Isolation**
- **Logical Partitioning**: Every record (Users, Leaves, Policies) is tagged with a `TenantId`.
- **Query Filtering**: The system enforces tenant isolation at the service layer, ensuring that even a Super Admin can only see data belonging to their specific environment unless explicitly operating at the Global system level.

---

## Database Schema Explanation

### **Core Identity & Organization**
- **`Tenants`**: The root of multi-tenancy. Every piece of data belongs to a Tenant (Company), ensuring complete data isolation between different clients.
- **`Users`**: Stores employee profiles, manager associations, and current status (Active/Pending).
- **`Roles`**: Implements **RBAC** (Role-Based Access Control). Stores permissions as a JSON blob to allow for highly granular and dynamic permission management.
- **`Departments`**: Allows grouping of users for organizational structure and policy targeting.

### **Leave Configuration**
- **`LeaveTypes`**: Defines categories like "Sick Leave" or "Annual Leave" and their base configurations.
- **`LeavePolicies`**: The "Brain" of the system. We use three types:
    - **Balance Policy**: Rules for accruals and carry-forwards.
    - **Usage Policy**: Rules for how many days can be taken (e.g., notice periods).
    - **Week-Off Policy**: Defines which days are non-working for specific groups.
- **`PolicyScopes`**: Acts as a "Router." It maps specific policies to specific Users, Roles, or Departments, allowing for complex rule-sets (e.g., "Managers in India get different sick leave than HR in USA").
- **`Holidays`**: Stores company-specific holidays that are subtracted from leave duration calculations.

### **Transaction & Workflow**
- **`LeaveRequests`**: The main record of a user applying for time off.
- **`LeaveApprovalSteps`**: Represents the **dynamic approval chain**. Each step can be assigned to a specific User or a Role. This allows for multi-stage approvals (e.g., Manager -> HR -> Finance).
- **`LeaveBalances`**: Tracks how many days a user has left for each leave type in a specific year.

### **System Utilities**
- **`UserInvites`**: Stores temporary, secure tokens for new users to set their passwords and activate their accounts.
- **`BulkUserInvites`**: Tracks the status and results of batch employee uploads (Excel/CSV).
- **`Notifications`**: Powers the in-app notification center for real-time updates on leave approvals or rejections.

---

## Key Features
- **Multi-Tenant Isolation**: No data leakage between companies.
- **Dynamic Policy Engine**: Policy resolution based on user priority and scope.
- **Auto-Approval Logic**: Workflow steps can auto-approve after a set number of days.
- **Sandwich Policy Support**: Handles weekends/holidays falling between leave days.
