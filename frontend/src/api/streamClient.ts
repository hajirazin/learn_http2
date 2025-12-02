export interface RecordDto {
  Id: number;
  Name: string;
  Value: number;
  CreatedAt: string;
}

/**
 * Streams records from the backend HTTP/2 NDJSON endpoint.
 * Yields records incrementally as they arrive over the network.
 */
export async function* streamRecords(apiUrl: string, signal?: AbortSignal): AsyncGenerator<RecordDto, void, unknown> {
  const response = await fetch(apiUrl, { signal });
  
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const reader = response.body?.getReader();
  if (!reader) {
    throw new Error('Response body is not readable');
  }

  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      
      if (done) {
        // Process any remaining data in the buffer
        if (buffer.trim()) {
          try {
            const record = JSON.parse(buffer.trim()) as RecordDto;
            yield record;
          } catch (e) {
            console.error('Failed to parse final buffer:', e);
          }
        }
        break;
      }

      // Decode the chunk and add to buffer
      buffer += decoder.decode(value, { stream: true });

      // Split by newlines and process complete lines
      const lines = buffer.split('\n');
      
      // Keep the last incomplete line in the buffer
      buffer = lines.pop() || '';

      // Parse and yield each complete line
      for (const line of lines) {
        const trimmed = line.trim();
        if (trimmed) {
          try {
            const record = JSON.parse(trimmed) as RecordDto;
            yield record;
          } catch (e) {
            console.error('Failed to parse line:', trimmed, e);
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

