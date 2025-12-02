# HTTP/2 Streaming Learning Project

A minimal monorepo demonstrating HTTP/2 streaming with a .NET backend and React frontend. This project showcases how to stream large datasets incrementally over HTTP/2 using `IAsyncEnumerable<T>` on the server and consuming the stream chunk-by-chunk in a React table.

## 🎯 Project Goal

Learn and experiment with HTTP/2 streaming by:
- Building a .NET 10 API that streams 100,000 records using `IAsyncEnumerable<T>`
- Consuming the stream in React using the Fetch API's `ReadableStream`
- Displaying records in an Ant Design table with pagination as they arrive
- Observing real-time incremental data loading

## 📁 Repository Structure

```
learn_http2/
├── backend/           # .NET 10 Minimal API with HTTP/2 streaming
│   ├── Models/        # RecordDto data model
│   ├── Services/      # RecordGenerator with IAsyncEnumerable
│   └── Program.cs     # API endpoint configuration
├── frontend/          # React + TypeScript + Ant Design
│   └── src/
│       ├── api/       # Streaming client using Fetch API
│       └── App.tsx    # Table UI with pagination
├── README.md          # This file
└── claude.md          # AI assistant context and constraints
```

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)

### Running the Backend

The backend streams 100,000 records in chunks of 1,000 with a 500ms delay between chunks, simulating real-world streaming scenarios.

```bash
cd backend
dotnet restore
dotnet run
```

The API will start on:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`

**Streaming endpoint**: `GET https://localhost:5001/api/records/stream`

The endpoint returns NDJSON (newline-delimited JSON) where each line is a complete `RecordDto` object. This format is ideal for streaming as records can be parsed incrementally without waiting for the entire response.

### Running the Frontend

The frontend connects to the backend via HTTPS and displays records in an Ant Design table with client-side pagination (100 records per page).

```bash
cd frontend
npm install
npm run dev
```

The React app will start on `http://localhost:3000`

Open your browser and navigate to the URL shown in the terminal. You'll see:
- A status indicator showing connection state and record count
- An Ant Design table that fills up as records stream in
- Pagination controls (100 records per page, so 1,000 records = 10 pages)

## 🔍 How It Works

### Backend: Streaming with IAsyncEnumerable<T>

The backend uses ASP.NET Core's native support for streaming via `IAsyncEnumerable<T>`:

```csharp
app.MapGet("/api/records/stream", async IAsyncEnumerable<RecordDto> () =>
{
    await foreach (var record in RecordGenerator.GenerateRecordsAsync())
    {
        yield return record;
    }
});
```

The `RecordGenerator` yields 1,000 records, delays 500ms, yields another 1,000, and so on until 100,000 records are sent. ASP.NET automatically serializes each record as NDJSON and flushes it to the client over HTTP/2.

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

## 🎓 Learning Points

### HTTP/2 Benefits
- **Multiplexing**: Multiple streams over a single connection
- **Server Push**: (not used here, but possible)
- **Header Compression**: Reduces overhead for repeated requests

### Streaming Benefits
- **Lower Time-to-First-Byte**: Users see data immediately
- **Reduced Memory**: Server doesn't buffer entire dataset
- **Better UX**: Progressive loading instead of long waits

### NDJSON Format
- Each line is a complete JSON object
- Easy to parse incrementally without complex state machines
- Widely supported for streaming APIs (e.g., Twitter, OpenAI)

## 🔧 Intentional Simplifications

This is a learning project, so we deliberately kept it simple:

- **No Database**: Records are generated in-memory
- **No Authentication**: Open endpoint for easy testing
- **No Tests**: Focus on learning HTTP/2, not TDD
- **No ESLint**: Minimal tooling overhead
- **No State Management**: Just React `useState`
- **No Error Retry**: Basic error handling only

## 📝 API Reference

### GET /api/records/stream

Streams 100,000 records in NDJSON format over HTTP/2.

**Response Format**: `application/x-ndjson`

Each line is a JSON object:
```json
{"id":1,"name":"Record 1","value":42,"createdAt":"2025-12-02T10:30:00Z"}
{"id":2,"name":"Record 2","value":84,"createdAt":"2025-12-02T10:30:00Z"}
```

**Behavior**:
- Yields 1,000 records immediately
- Delays 500ms
- Yields next 1,000 records
- Repeats until 100,000 records are sent

## 🐛 Troubleshooting

### "Failed to fetch" error
- Ensure the backend is running on `https://localhost:5001`
- Check if your browser blocks self-signed certificates (accept the certificate warning)

### Table doesn't update
- Check browser console for JavaScript errors
- Verify the NDJSON format is correct (each line must be valid JSON)

### Backend doesn't support HTTP/2
- Ensure you're using `dotnet run` (not IIS Express)
- Verify Kestrel is configured for HTTPS (required for HTTP/2)
- Check `launchSettings.json` has an `https` profile

## 📚 Further Reading

- [HTTP/2 Specification](https://httpwg.org/specs/rfc7540.html)
- [ASP.NET Core Streaming](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types#iasyncenumerablet-type)
- [Fetch API Streams](https://developer.mozilla.org/en-US/docs/Web/API/Streams_API)
- [NDJSON Format](http://ndjson.org/)

## 📄 License

This is a learning project - use it however you want!
