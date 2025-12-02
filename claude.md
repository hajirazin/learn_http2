# Claude AI Context - HTTP/2 Streaming Learning Project

This file provides context and constraints for AI assistants working with this codebase.

## Project Purpose

This is a **learning project** focused on understanding HTTP/2 streaming mechanics. The goal is educational simplicity, not production readiness. We deliberately avoid common production patterns (auth, DB, testing) to keep the focus on the streaming behavior itself.

## Core Constraints

### What This Project IS
- A minimal demonstration of HTTP/2 streaming with `IAsyncEnumerable<T>`
- A learning sandbox for observing incremental data delivery
- A simple backend + frontend setup with no infrastructure dependencies

### What This Project IS NOT
- A production-ready application
- A template for building real-world APIs
- A showcase of best practices (intentionally simplified)

## Technical Decisions

### Backend (.NET 10)

**Framework**: ASP.NET Core Minimal API
- No controllers, no layers, no abstractions
- Single `Program.cs` with endpoint definitions
- Direct use of `IAsyncEnumerable<T>` return type

**Data Generation**: In-memory only
- `RecordGenerator.GenerateRecordsAsync()` yields 100,000 synthetic records
- Chunks of 1,000 records with 500ms delay between chunks
- **Do not** add Entity Framework, Dapper, or any database
- **Do not** add repository patterns or data access layers

**HTTP/2 Configuration**:
- Kestrel is configured via `launchSettings.json` to use HTTPS
- HTTP/2 is automatically enabled for HTTPS in .NET
- CORS is enabled for local development (`http://localhost:3000`)

**Response Format**: NDJSON (Newline-Delimited JSON)
- Each record is serialized as a single line of JSON
- Newline character separates records
- No wrapping array, no commas between records
- This format allows incremental parsing on the client

### Frontend (React + TypeScript + Ant Design)

**Build Tool**: Vite
- Fast dev server with HMR
- TypeScript for type safety
- **No** ESLint configuration (intentionally omitted)
- **No** testing framework (learning focus, not QA)

**UI Library**: Ant Design
- Chosen for professional-looking table with pagination out-of-the-box
- Minimal configuration required
- Good TypeScript support

**Streaming Client**: Fetch API + ReadableStream
- `streamClient.ts` implements an async generator that yields records
- Reads chunks from `response.body.getReader()`
- Parses NDJSON incrementally (split by newlines)
- Exposes records via `for await` loop for easy consumption

**State Management**: React `useState` only
- Records stored in a simple array
- Array grows as streaming continues
- Table re-renders automatically when state updates
- **Do not** add Redux, Zustand, or other state libraries

**Pagination**: Ant Design Table built-in
- Page size: **100 records per page**
- Client-side pagination (all records in memory)
- Shows total count so users can see new pages appearing
- Each 1,000-record chunk from backend fills 10 pages

## File Structure & Conventions

```
backend/
  ├── Models/
  │   └── RecordDto.cs          # Simple DTO with Id, Name, Value, CreatedAt
  ├── Services/
  │   └── RecordGenerator.cs    # IAsyncEnumerable generator with delays
  ├── Program.cs                # Minimal API endpoint + CORS config
  ├── Http2Streaming.Api.csproj # .NET 10 project file
  └── Properties/
      └── launchSettings.json   # Kestrel HTTPS profile

frontend/
  ├── src/
  │   ├── api/
  │   │   └── streamClient.ts   # Fetch + ReadableStream + NDJSON parser
  │   ├── App.tsx               # Main component with Ant Design Table
  │   ├── App.css               # Minimal styling
  │   ├── index.css             # Global styles
  │   └── main.tsx              # React entry point
  ├── vite.config.ts            # Dev server on port 3000
  └── package.json              # Dependencies: react, antd
```

## Key Endpoints

### `GET https://localhost:5001/api/records/stream`

Returns NDJSON stream of 100,000 records.

**Behavior**:
1. Yields 1,000 records
2. Waits 500ms (`await Task.Delay(500)`)
3. Yields next 1,000 records
4. Repeats 100 times (100k total)

**Response Headers**:
- `Content-Type: application/x-ndjson`
- No `Content-Length` (streaming, unknown length)
- HTTP/2 protocol

**Example Response**:
```
{"id":1,"name":"Record 1","value":42,"createdAt":"2025-12-02T10:30:00Z"}
{"id":2,"name":"Record 2","value":84,"createdAt":"2025-12-02T10:30:00Z"}
...
```

## Data Contract

### RecordDto

```csharp
public record RecordDto(
    int Id,
    string Name,
    double Value,
    DateTime CreatedAt
);
```

```typescript
export interface RecordDto {
  id: number;
  name: string;
  value: number;
  createdAt: string; // ISO 8601 date string
}
```

## Things NOT to Add

When working with this codebase, **do not** add:

1. **Database or ORM**: No Entity Framework, Dapper, SQLite, etc.
2. **Authentication**: No JWT, OAuth, API keys, etc.
3. **Authorization**: No role-based access control
4. **Logging Framework**: Console output is sufficient
5. **Unit Tests**: This is a learning project, not TDD
6. **Integration Tests**: Keep it simple
7. **ESLint**: Intentionally omitted to reduce tooling complexity
8. **Prettier**: Not configured
9. **Docker**: Run directly with `dotnet run` and `npm run dev`
10. **CI/CD**: No GitHub Actions, no deployment pipelines
11. **Environment Variables**: Hardcode localhost URLs
12. **Validation**: No FluentValidation, no complex error handling
13. **Swagger/OpenAPI**: Not needed for a single endpoint
14. **Monitoring**: No Application Insights, no telemetry
15. **Rate Limiting**: No throttling, no quotas
16. **Caching**: No Redis, no in-memory cache

## Common Tasks

### Adding a New Field to RecordDto

1. Update `backend/Models/RecordDto.cs` record definition
2. Update `backend/Services/RecordGenerator.cs` to populate the field
3. Update `frontend/src/api/streamClient.ts` TypeScript interface
4. Update `frontend/src/App.tsx` to add a new table column

### Changing Chunk Size or Delay

Edit `backend/Services/RecordGenerator.cs`:
```csharp
private const int ChunkSize = 1000;  // Change this
private const int DelayMs = 500;     // Change this
```

### Changing Page Size

Edit `frontend/src/App.tsx`:
```typescript
pagination={{
  pageSize: 100,  // Change this
  // ...
}}
```

### Changing Total Record Count

Edit `backend/Services/RecordGenerator.cs`:
```csharp
private const int TotalRecords = 100_000;  // Change this
```

## Testing the Streaming Behavior

### Backend Verification

```bash
# From the backend directory
dotnet run

# In another terminal, use curl to see raw NDJSON stream
curl -k https://localhost:5001/api/records/stream

# You should see records appearing in chunks with 500ms delays
```

### Frontend Verification

1. Start backend: `cd backend && dotnet run`
2. Start frontend: `cd frontend && npm run dev`
3. Open `http://localhost:3000` in browser
4. Open browser DevTools Network tab
5. Look for the `/api/records/stream` request
6. Click on it and view the "Response" tab
7. You should see records appearing incrementally, not all at once

### Visual Confirmation

Watch the status line in the UI:
- "🔌 Connecting to server..." → Initial fetch
- "📡 Streaming... (1,000 records received)" → First chunk arrived
- "📡 Streaming... (2,000 records received)" → Second chunk (500ms later)
- "✅ Stream completed! (100,000 total records)" → Done

Watch the pagination:
- Initially: "1-100 of 1,000" (first page of first chunk)
- After 500ms: "1-100 of 2,000" (first page, but second chunk loaded)
- After 1s: "1-100 of 3,000" (growing in background)

## HTTP/2 Verification

To confirm HTTP/2 is actually being used:

1. Open Chrome DevTools → Network tab
2. Right-click column headers → Enable "Protocol" column
3. Reload the page
4. Look for "h2" in the Protocol column for `/api/records/stream`

If you see "http/1.1", check:
- Backend is running with HTTPS (`https://localhost:5001`)
- Browser accepted the self-signed certificate
- Kestrel is configured correctly in `launchSettings.json`

## Troubleshooting

### CORS Error
- Check `Program.cs` allows origin `http://localhost:3000`
- Ensure frontend runs on port 3000 (configured in `vite.config.ts`)

### Self-Signed Certificate
- Navigate to `https://localhost:5001` in browser first
- Accept the certificate warning
- Then refresh the frontend

### Stream Not Updating UI
- Check browser console for JavaScript errors
- Verify NDJSON format (use `curl` to inspect raw response)
- Ensure each line is valid JSON with no trailing commas

### Too Slow / Too Fast
- Adjust `DelayMs` in `RecordGenerator.cs`
- Adjust `ChunkSize` to change how often UI updates

## Future Learning Paths

If you want to extend this project for more learning:

1. **Server-Sent Events (SSE)**: Compare with NDJSON streaming
2. **WebSockets**: Compare with HTTP/2 streaming
3. **gRPC Streaming**: Compare with REST streaming
4. **Backpressure**: Implement client-side flow control
5. **Progress Indicators**: Add progress bar based on known total
6. **Cancellation**: Add "Stop Streaming" button
7. **Reconnection**: Handle network interruptions gracefully

But remember: keep additions simple and focused on learning!

## Questions to Explore

- How much faster is HTTP/2 vs HTTP/1.1 for streaming?
- What happens if the client is slower than the server?
- How much memory does the browser use with 100k records?
- Can you stream 1 million records? 10 million?
- What's the impact of changing chunk size on perceived performance?

## Summary

This project is intentionally minimal. Resist the urge to "improve" it with production patterns. The goal is to understand HTTP/2 streaming, not to build a real application. Keep it simple, keep it focused, and learn by observing the behavior!

