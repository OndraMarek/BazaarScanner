import { useState, useEffect } from 'react';
import type { ScannedItem } from '../pages/Home';

interface EditItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  item: ScannedItem | null;
  onSaveSuccess: () => void;
}

function EditItemModal({
  isOpen,
  onClose,
  item,
  onSaveSuccess,
}: EditItemModalProps) {
  const [name, setName] = useState('');
  const [type, setType] = useState('Other');
  const [count, setCount] = useState(1);

  useEffect(() => {
    if (item && isOpen) {
      setName(item.name);
      setType(item.type);
      setCount(item.count);
    }
  }, [item, isOpen]);

  if (!isOpen || !item) return null;

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();

    const requestBody = {
      id: item.id,
      name,
      type,
      count,
      imageUrl: item.imageUrl,
    };

    try {
      const response = await fetch(
        `https://localhost:7102/api/items/${item.id}`,
        {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(requestBody),
        },
      );

      if (response.ok) {
        onSaveSuccess();
        onClose();
      } else {
        alert('Failed to update item.');
      }
    } catch (error) {
      console.error('Update failed:', error);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4">
      <div className="bg-sky-900 rounded-xl shadow-2xl p-6 w-full max-w-md text-left text-white">
        <h2 className="text-2xl font-bold mb-4 border-b border-sky-700 pb-2">
          Edit Item
        </h2>

        <form onSubmit={handleSave} className="flex flex-col gap-3">
          <label className="flex flex-col text-sm font-medium">
            Name:
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 p-2 rounded bg-sky-950 border border-sky-700"
            />
          </label>

          <label className="flex flex-col text-sm font-medium">
            Category:
            <select
              value={type}
              onChange={(e) => setType(e.target.value)}
              className="mt-1 p-2 rounded bg-sky-950 border border-sky-700"
            >
              <option value="Other">Other</option>
              <option value="Electronic">Electronic</option>
              <option value="Book">Book</option>
              <option value="Clothing">Clothing</option>
              <option value="Toy">Toy</option>
              <option value="Media">Media</option>
            </select>
          </label>

          <label className="flex flex-col text-sm font-medium">
            Count:
            <input
              type="number"
              min="1"
              value={count}
              onChange={(e) => setCount(Number(e.target.value))}
              className="mt-1 p-2 rounded bg-sky-950 border border-sky-700"
            />
          </label>

          <div className="mt-4 flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 bg-gray-600 rounded hover:bg-gray-500 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="flex-1 px-4 py-2 bg-yellow-600 hover:bg-yellow-500 rounded font-bold transition-colors"
            >
              Save Changes
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default EditItemModal;
