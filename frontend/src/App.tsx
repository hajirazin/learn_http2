import { useState, useEffect } from 'react';
import { Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { streamRecords } from './api/streamClient';
import type { RecordDto } from './api/streamClient';
import 'antd/dist/reset.css';
import './App.css';

const columns: ColumnsType<RecordDto> = [
  {
    title: 'ID',
    dataIndex: 'Id',
    key: 'Id',
    width: 100,
  },
  {
    title: 'Name',
    dataIndex: 'Name',
    key: 'Name',
    width: 200,
  },
  {
    title: 'Value',
    dataIndex: 'Value',
    key: 'Value',
    width: 150,
  },
  {
    title: 'Created At',
    dataIndex: 'CreatedAt',
    key: 'CreatedAt',
    render: (text: string) => new Date(text).toLocaleString(),
  },
];

function App() {
  const [records, setRecords] = useState<RecordDto[]>([]);
  const [status, setStatus] = useState<'connecting' | 'streaming' | 'completed' | 'error'>('connecting');
  const [errorMessage, setErrorMessage] = useState<string>('');

  useEffect(() => {
    const abortController = new AbortController();
    
    setRecords([]);
    setStatus('connecting');
    
    const loadRecords = async () => {
      try {
        const apiUrl = 'https://localhost:5001/api/records/stream';
        
        setStatus('streaming');

        const batchSize = 1000;
        let batch: RecordDto[] = [];
        
        for await (const record of streamRecords(apiUrl, abortController.signal)) {
          // Check if we should abort
          if (abortController.signal.aborted) {
            console.log('Stream aborted');
            setStatus('completed');
            return;
          }

          batch.push(record);
          if (batch.length >= batchSize) {
            const batchToAdd = batch; // Capture reference before clearing
            setRecords(prev => [...prev, ...batchToAdd]);
            console.log(`Added ${batchToAdd.length} records to the table`);
            batch = []; // Now safe to clear
          }
        }

        if (batch.length > 0) {
          const finalBatch = batch;
          setRecords(prev => [...prev, ...finalBatch]);
          console.log(`Final batch: Added ${finalBatch.length} records to the table`);
        }
        
        setStatus('completed');
      } catch (error) {
        // Don't show error if we aborted intentionally
        if (error instanceof Error && error.name === 'AbortError') {
          console.log('Stream cancelled');
          setStatus('completed');
          return;
        }
        
        console.error('Error streaming records:', error);
        setStatus('error');
        setErrorMessage(error instanceof Error ? error.message : 'Unknown error occurred');
      }
    };

    loadRecords();
    
    // Cleanup for real unmounts (e.g., navigation away from page)
    return () => {
      abortController.abort();
    };
  }, []);

  const getStatusMessage = () => {
    switch (status) {
      case 'connecting':
        return '🔌 Connecting to server...';
      case 'streaming':
        return `📡 Streaming... (${records.length.toLocaleString()} records received)`;
      case 'completed':
        return `✅ Stream completed! (${records.length.toLocaleString()} total records)`;
      case 'error':
        return `❌ Error: ${errorMessage}`;
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <h1>HTTP/2 Streaming Demo</h1>
      <div style={{ marginBottom: '16px', fontSize: '16px', fontWeight: 'bold' }}>
        {getStatusMessage()}
      </div>
      
      <Table
        columns={columns}
        dataSource={records}
        rowKey="Id"
        pagination={{
          pageSize: 100,
          showSizeChanger: false,
          showTotal: (total, range) => `${range[0]}-${range[1]} of ${total} records`,
        }}
        loading={status === 'connecting'}
        scroll={{ y: 600 }}
      />
    </div>
  );
}

export default App;
