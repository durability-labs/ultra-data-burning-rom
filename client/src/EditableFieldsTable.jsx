import React, { useState } from 'react';

export default function EditableFieldsTable() {
  const [fields, setFields] = useState({
    title: '',
    author: '',
    tags: '',
    description: ''
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFields(prev => ({ ...prev, [name]: value }));
  };

  return (
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto', marginTop: '1.5rem' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <tbody>
          <tr>
            <td style={{ padding: '8px', fontWeight: 'bold', width: '120px', color: '#fff' }}>Title</td>
            <td style={{ padding: '8px' }}>
              <input
                type="text"
                name="title"
                value={fields.title}
                onChange={handleChange}
                style={{ width: '100%', padding: '6px', borderRadius: '4px', border: '1px solid #ccc' }}
              />
            </td>
          </tr>
          <tr>
            <td style={{ padding: '8px', fontWeight: 'bold', color: '#fff' }}>Author</td>
            <td style={{ padding: '8px' }}>
              <input
                type="text"
                name="author"
                value={fields.author}
                onChange={handleChange}
                style={{ width: '100%', padding: '6px', borderRadius: '4px', border: '1px solid #ccc' }}
              />
            </td>
          </tr>
          <tr>
            <td style={{ padding: '8px', fontWeight: 'bold', color: '#fff' }}>Tags</td>
            <td style={{ padding: '8px' }}>
              <input
                type="text"
                name="tags"
                value={fields.tags}
                onChange={handleChange}
                style={{ width: '100%', padding: '6px', borderRadius: '4px', border: '1px solid #ccc' }}
              />
            </td>
          </tr>
          <tr>
            <td style={{ padding: '8px', fontWeight: 'bold', color: '#fff' }}>Description</td>
            <td style={{ padding: '8px' }}>
              <textarea
                name="description"
                value={fields.description}
                onChange={handleChange}
                style={{ width: '100%', padding: '6px', borderRadius: '4px', border: '1px solid #ccc', minHeight: '80px' }}
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}