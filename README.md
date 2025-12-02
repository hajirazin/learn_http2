# HTTP/2 Streaming Learning Project

A monorepo demonstrating HTTP/2 streaming with a .NET backend, React frontend, and PostgreSQL database. This project showcases how to stream large datasets incrementally over HTTP/2 using `IAsyncEnumerable<T>` from a database and consuming the stream chunk-by-chunk in a React table.

## 🎯 Project Goal

Learn and experiment with HTTP/2 streaming by:
- Building a .NET 10 API that streams 100,000 records from PostgreSQL using `IAsyncEnumerable<T>`
- Using Entity Framework Core with efficient streaming queries
- Running PostgreSQL in Docker with automated migrations and seeding
- Consuming the stream in React using the Fetch API's `ReadableStream`
- Displaying records in an Ant Design table with pagination as they arrive
- Observing real-time incremental data loading from a real database

## 📁 Repository Structure

```
learn_http2/
├── backend/              # .NET 10 Minimal API with HTTP/2 streaming
│   ├── Data/            # EF Core DbContext and entities
│   │   ├── AppDbContext.cs
│   │   ├── AppDbContextFactory.cs
│   │   └── Entities/    # Database entity models
│   ├── Migrations/      # EF Core migrations (includes 100k record seeding)
│   ├── Models/          # RecordDto data transfer objects
│   ├── appsettings.json # Configuration with connection strings
│   └── Program.cs       # API endpoints and startup configuration
├── frontend/            # React + TypeScript + Ant Design
│   └── src/
│       ├── api/         # Streaming client using Fetch API
│       └── App.tsx      # Table UI with pagination
├── docker-compose.yml   # PostgreSQL container configuration
├── Makefile             # Development workflow commands
├── .env.example         # Environment variable template
└── README.md            # This file
```

## 🏗️ Architecture

```
┌─────────────────┐      HTTPS/HTTP2       ┌──────────────────┐
│                 │  ──────────────────>    │                  │
│  React Frontend │                         │  .NET Backend    │
│  (localhost:3000)│  <──────────────────    │  (localhost:5001)│
└─────────────────┘    NDJSON Stream       └──────────────────┘
                                                      │
                                                      │ EF Core
                                                      │ Streaming
                                                      ▼
                                            ┌──────────────────┐
                                            │  PostgreSQL DB   │
                                            │  (Docker)        │
                                            │  localhost:5432  │
                                            │  100k Records    │
                                            └──────────────────┘
```

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **Docker & Docker Compose** - [Download here](https://www.docker.com/products/docker-desktop)
- **Make** - (Pre-installed on macOS/Linux, [install on Windows](https://gnuwin32.sourceforge.net/packages/make.htm))

### Quick Start (First Time Setup)

1. **Clone the repository** and navigate to the project root

2. **Create environment file** (optional, defaults will work):
   ```bash
   # The default credentials work out of the box
   # For production, create a .env file:
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=your_secure_password
   POSTGRES_DB=http2streaming
   POSTGRES_PORT=5432
   ```

3. **Run the setup command**:
   ```bash
   make setup
   ```
   This will:
   - Start PostgreSQL in Docker
   - Restore .NET dependencies
   - Install npm dependencies
   - Run database migrations (creates table and seeds 100k records)

4. **Start the backend** (in terminal 1):
   ```bash
   make backend-run
   ```
   The API will start on `https://localhost:5001`

5. **Start the frontend** (in terminal 2):
   ```bash
   make frontend-dev
   ```
   The React app will start on `http://localhost:3000`

6. **Open your browser** and navigate to `http://localhost:3000`

### Development Workflow

After the initial setup, you can use these commands:

```bash
# Start database only (backend and frontend run separately)
make dev

# Then in separate terminals:
make backend-run   # Terminal 1
make frontend-dev  # Terminal 2

# Check health of all services
make health

# View database logs
make db-logs

# Access database shell
make db-shell
```

### Available Make Commands

Run `make help` to see all available commands:

**Database (Docker) Commands:**
- `make db-up` - Start Postgres container
- `make db-down` - Stop Postgres container
- `make db-restart` - Restart Postgres container
- `make db-logs` - View Postgres logs
- `make db-shell` - Open psql shell
- `make db-clean` - Remove container and volumes (deletes all data!)

**Backend Commands (Local):**
- `make backend-restore` - Restore .NET dependencies
- `make backend-build` - Build backend project
- `make backend-run` - Run backend on port 5001
- `make backend-migrate` - Apply EF Core migrations
- `make backend-migration NAME=<name>` - Create new migration
- `make backend-reset-db` - Drop database and re-run migrations

**Frontend Commands (Local):**
- `make frontend-install` - Install npm dependencies
- `make frontend-dev` - Run dev server on port 3000
- `make frontend-build` - Build for production

**Combined Workflows:**
- `make setup` - First-time setup (recommended)
- `make dev` - Start database (run backend/frontend manually)
- `make clean-all` - Stop and clean everything
- `make health` - Check service health

### Port Mappings

| Service    | Port | URL |
|------------|------|-----|
| Frontend   | 3000 | http://localhost:3000 |
| Backend    | 5001 | https://localhost:5001 |
| PostgreSQL | 5432 | localhost:5432 |

### Streaming Endpoint

**GET** `https://localhost:5001/api/records/stream`

Returns NDJSON (newline-delimited JSON) where each line is a complete `RecordDto` object. The backend streams all 100,000 records from the database using EF Core's `AsAsyncEnumerable()` for efficient memory usage.

### Health Check Endpoint

**GET** `https://localhost:5001/health`

Returns health status with database connectivity check:
```json
{
  "status": "Healthy",
  "database": "Connected",
  "recordCount": 100000,
  "timestamp": "2024-12-02T10:30:00Z"
}
```

## 🔍 How It Works

### Database: PostgreSQL with EF Core

The application uses PostgreSQL running in Docker with Entity Framework Core for data access:

- **100,000 records** are seeded during the initial migration
- Records are stored in a `Records` table with indexed `Id` column
- **Efficient streaming** using `.AsNoTracking()` and `.AsAsyncEnumerable()`
- **Connection pooling** for optimal performance

### Backend: Streaming with IAsyncEnumerable<T>

The backend streams records directly from the database using EF Core:

```csharp
await foreach (var recordEntity in dbContext.Records
    .AsNoTracking()
    .OrderBy(r => r.Id)
    .AsAsyncEnumerable()
    .WithCancellation(cancellationToken))
{
    var record = new RecordDto(/* map entity to DTO */);
    var json = JsonSerializer.Serialize(record);
    await context.Response.WriteAsync(json + "\n", cancellationToken);
    await context.Response.Body.FlushAsync(cancellationToken);
}
```

Key optimizations:
- **`.AsNoTracking()`** - Disables EF change tracking for read-only queries (faster)
- **`.AsAsyncEnumerable()`** - Streams results without loading all into memory
- **`.WithCancellation()`** - Respects client disconnects
- **NDJSON format** - Each record is immediately flushed to the client

### Frontend: Consuming the Stream

The React app uses the Fetch API's `ReadableStream` to read chunks as they arrive:

1. **Fetch the endpoint** and get a `ReadableStream`
2. **Read chunks** from the stream using a `ReadableStreamDefaultReader`
3. **Parse NDJSON** by splitting on newlines and parsing each line as JSON
4. **Yield records** one at a time via an `async generator` function
5. **Update state** incrementally so the table re-renders with new data

The streaming client is implemented in `frontend/src/api/streamClient.ts` and consumed in `App.tsx` using a `for await` loop.

## 🎨 Frontend Features

- **Ant Design Table**: Professional data grid with sorting, filtering, and pagination
- **Page Size: 100**: Each page shows 100 records, making it easy to see new pages filling up
- **Real-time Updates**: The table re-renders as each record arrives (batched by React)
- **Status Indicator**: Shows connection state and total records received
- **No Total Count Confusion**: The pagination shows the actual count as it grows

## 🗄️ Database Schema

```sql
CREATE TABLE "Records" (
    "Id" integer NOT NULL PRIMARY KEY,
    "Name" character varying(200) NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);

CREATE INDEX "IX_Records_Id" ON "Records" ("Id");
```

The migration automatically seeds 100,000 records with:
- Sequential IDs (1 to 100,000)
- Names like "Item 1", "Item 2", etc.
- Random decimal values between 0 and 1000
- Sequential timestamps for predictable ordering

## 🎓 Learning Points

### HTTP/2 Benefits
- **Multiplexing**: Multiple streams over a single connection
- **Server Push**: (not used here, but possible)
- **Header Compression**: Reduces overhead for repeated requests

### Database Streaming Benefits
- **Memory Efficient**: Records are streamed from DB, not loaded into memory
- **EF Core AsAsyncEnumerable**: Native support for streaming queries
- **AsNoTracking**: Disables change tracking for read-only queries (30-40% faster)
- **Cancellation Tokens**: Properly handles client disconnects

### Streaming Benefits
- **Lower Time-to-First-Byte**: Users see data immediately
- **Reduced Memory**: Server doesn't buffer entire dataset
- **Better UX**: Progressive loading instead of long waits
- **Scalable**: Can stream millions of records without OOM

### NDJSON Format
- Each line is a complete JSON object
- Easy to parse incrementally without complex state machines
- Widely supported for streaming APIs (e.g., Twitter, OpenAI)

### Docker & Development Workflow
- **Database isolation**: Postgres runs in Docker, easy to reset
- **Makefile automation**: Simple commands for complex workflows
- **Migration on startup**: Database auto-updates when backend starts
- **Health checks**: Verify all services are running correctly

## 🔧 Design Decisions

### What We Included
- ✅ **PostgreSQL Database**: Real-world database streaming scenario
- ✅ **Entity Framework Core**: Industry-standard ORM with migrations
- ✅ **Docker Compose**: Easy database setup and teardown
- ✅ **Makefile**: Professional development workflow
- ✅ **Health Checks**: Production-ready monitoring endpoint
- ✅ **Migration Seeding**: Automated 100k record generation

### Intentional Simplifications
This is a learning project focused on HTTP/2 streaming and database efficiency:

- **No Authentication**: Open endpoint for easy testing
- **No Tests**: Focus on learning streaming patterns, not TDD
- **No ESLint**: Minimal tooling overhead
- **No State Management**: Just React `useState`
- **Backend/Frontend run locally**: Only database is containerized
- **Basic Error Handling**: No retry logic or circuit breakers

## 📝 API Reference

### GET /

Returns a simple text response to verify the backend is running.

**Response**: `text/plain`
```
HTTP/2 streaming demo backend is running.
```

### GET /health

Health check endpoint with database connectivity verification.

**Response**: `application/json`

Success (200):
```json
{
  "status": "Healthy",
  "database": "Connected",
  "recordCount": 100000,
  "timestamp": "2024-12-02T10:30:00Z"
}
```

Failure (503):
```json
{
  "status": "Unhealthy",
  "database": "Cannot connect",
  "timestamp": "2024-12-02T10:30:00Z"
}
```

### GET /api/records/stream

Streams all records from the database in NDJSON format over HTTP/2.

**Response Format**: `application/x-ndjson`

Each line is a JSON object:
```json
{"Id":1,"Name":"Item 1","Value":42.50,"CreatedAt":"2024-01-01T00:00:01Z"}
{"Id":2,"Name":"Item 2","Value":123.75,"CreatedAt":"2024-01-01T00:00:02Z"}
```

**Behavior**:
- Streams records directly from database using EF Core
- Uses `.AsNoTracking()` for optimal performance
- Records are ordered by `Id`
- No artificial delays - streams as fast as network allows
- Automatically handles cancellation when client disconnects

## 🐛 Troubleshooting

### Database Connection Issues

**Error**: "Failed to apply migrations" or "Cannot connect to database"
- Ensure Docker is running: `docker ps`
- Check if Postgres container is healthy: `make db-logs`
- Verify port 5432 is not in use: `lsof -i :5432` (macOS/Linux)
- Try restarting the database: `make db-restart`

### Backend Issues

**Error**: "Failed to fetch" or CORS errors
- Ensure the backend is running on `https://localhost:5001`
- Check if your browser blocks self-signed certificates (accept the certificate warning)
- Verify database is running: `make health`

**Error**: Build or restore failures
- Clean and restore: `cd backend && dotnet clean && dotnet restore`
- Check .NET SDK version: `dotnet --version` (should be 10.0+)

### Frontend Issues

**Error**: Table doesn't update
- Check browser console for JavaScript errors
- Verify the NDJSON format is correct (each line must be valid JSON)
- Ensure backend is streaming: Check Network tab in DevTools

**Error**: npm install failures
- Delete `node_modules` and `package-lock.json`
- Run `make frontend-install` again
- Check Node.js version: `node --version` (should be 18+)

### Docker Issues

**Error**: "Cannot connect to the Docker daemon"
- Ensure Docker Desktop is running
- On Linux: Check if Docker service is started

**Error**: Port already in use
- Check what's using the port: `lsof -i :5432`
- Stop conflicting service or change port in `.env`

### Database Reset

If you need to start fresh:
```bash
make db-clean          # Remove database and volumes
make db-up             # Start fresh database
make backend-migrate   # Run migrations and seed data
```

Or use the interactive reset:
```bash
make backend-reset-db  # Drops DB and re-runs migrations
```

## 📚 Further Reading

### HTTP/2 & Streaming
- [HTTP/2 Specification](https://httpwg.org/specs/rfc7540.html)
- [ASP.NET Core Streaming](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types#iasyncenumerablet-type)
- [Fetch API Streams](https://developer.mozilla.org/en-US/docs/Web/API/Streams_API)
- [NDJSON Format](http://ndjson.org/)

### Entity Framework Core
- [EF Core Streaming Queries](https://learn.microsoft.com/en-us/ef/core/querying/async)
- [AsNoTracking Performance](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)

### Docker & DevOps
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [Makefile Tutorial](https://makefiletutorial.com/)

## 📄 License

This is a learning project - use it however you want!
