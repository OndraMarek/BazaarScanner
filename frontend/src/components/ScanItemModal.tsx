import { useState } from 'react';
import type { ScannedItem } from '../pages/Home';

interface ScanItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void;
}

function ScanItemModal({ isOpen, onClose, onSaveSuccess }: ScanItemModalProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string>('');

  const [isScanning, setIsScanning] = useState(false);
  const [scannedItem, setScannedItem] = useState<ScannedItem | null>(null);

  const [name, setName] = useState('');
  const [type, setType] = useState('Other');
  const [count, setCount] = useState(1);

  if (!isOpen) return null;

  const resetModal = () => {
    setSelectedFile(null);
    setPreviewUrl('');
    setScannedItem(null);
    setName('');
    setType('Other');
    setCount(1);
    onClose();
  };

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const file = e.target.files[0];
      setSelectedFile(file);
      setPreviewUrl(URL.createObjectURL(file));

      await scanImage(file);
    }
  };

  const scanImage = async (file: File) => {
    setIsScanning(true);
    const formData = new FormData();
    formData.append('Image', file);

    try {
      const response = await fetch('https://localhost:7102/api/items/scan', {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        const data: ScannedItem = await response.json();
        setScannedItem(data);
        setName(data.name);
        setType(data.type);
        setCount(data.count);
      }
    } catch (error) {
      console.error('Scan failed:', error);
    } finally {
      setIsScanning(false);
    }
  };

  const handleRescan = async () => {
    if (!selectedFile || !scannedItem) return;
    setIsScanning(true);

    const wrongItem = { ...scannedItem, name, type, count };

    const formData = new FormData();
    formData.append('Image', selectedFile);
    formData.append('ItemOldJson', JSON.stringify(wrongItem));

    try {
      const response = await fetch('https://localhost:7102/api/items/rescan', {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        const data: ScannedItem = await response.json();
        setScannedItem(data);
        setName(data.name);
        setType(data.type);
        setCount(data.count);
      }
    } catch (error) {
      console.error('Rescan failed:', error);
    } finally {
      setIsScanning(false);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!scannedItem) return;

    const requestBody = {
      name,
      type,
      count,
      imageUrl: scannedItem.imageUrl,
    };

    try {
      const response = await fetch('https://localhost:7102/api/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody),
      });

      if (response.ok) {
        onSaveSuccess();
        resetModal();
      } else {
        const errorText = await response.text();
        console.error('API returned an error:', response.status, errorText);
        alert(`Failed to save the item. Status: ${response.status}`);
      }
    } catch (error) {
      console.error('Save failed:', error);
      alert('Network connection error. Is the backend running?');
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4">
      <div className="bg-sky-900 rounded-xl shadow-2xl p-6 w-full max-w-md text-left text-white max-h-[90vh] overflow-y-auto">
        <h2 className="text-2xl font-bold mb-4 border-b border-sky-700 pb-2">
          Add item
        </h2>

        {!selectedFile ? (
          <div className="flex flex-col items-center justify-center py-10">
            <label className="cursor-pointer bg-blue-600 hover:bg-blue-500 text-white px-6 py-4 rounded-lg font-bold text-center w-full shadow-lg">
              Take a photo / Select a photo
              <input
                type="file"
                accept="image/*"
                capture="environment"
                className="hidden"
                onChange={handleFileSelect}
              />
            </label>
            <button
              onClick={resetModal}
              className="mt-4 text-sky-300 underline"
            >
              Cancel
            </button>
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            <div className="relative w-full h-48 bg-black rounded-lg overflow-hidden">
              <img
                src={previewUrl}
                alt="Preview"
                className="w-full h-full object-contain"
              />
              {isScanning && (
                <div className="absolute inset-0 bg-black/60 flex flex-col items-center justify-center">
                  <div className="w-10 h-10 border-4 border-t-blue-500 border-white rounded-full animate-spin"></div>
                  <p className="mt-2 font-bold">AI analyzes...</p>
                </div>
              )}
            </div>

            {!isScanning && scannedItem && (
              <form onSubmit={handleSave} className="flex flex-col gap-3">
                <label className="flex flex-col text-sm font-medium">
                  Name (Edit if AI got it wrong):
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
                    <option value="Electronic">Electronics</option>
                    <option value="Book">Books</option>
                    <option value="Clothing">Clothing</option>
                    <option value="Toy">Toys</option>
                    <option value="Media">Media (CD/DVD)</option>
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

                <div className="mt-4 flex flex-col gap-3">
                  <button
                    type="button"
                    onClick={handleRescan}
                    className="w-full px-4 py-2 bg-orange-600 hover:bg-orange-500 text-white rounded font-bold"
                  >
                    AI missed, Try again (Rescan)
                  </button>

                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={resetModal}
                      className="flex-1 px-4 py-2 bg-gray-600 rounded"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      className="flex-1 px-4 py-2 bg-green-600 hover:bg-green-500 rounded font-bold"
                    >
                      Save item
                    </button>
                  </div>
                </div>
              </form>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default ScanItemModal;
