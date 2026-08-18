import { useState, useEffect } from 'react';
import ItemCard from '../components/ItemCard';
import ScanItemModal from '../components/ScanItemModal';
import EditItemModal from '../components/EditItemModal';

export interface ScannedItem {
  id: string;
  name: string;
  type: string;
  count: number;
  imageUrl?: string;
}

function Home() {
  const [items, setItems] = useState<ScannedItem[]>([]);
  const [isScanModalOpen, setIsScanModalOpen] = useState(false);

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<ScannedItem | null>(null);

  const fetchItems = async () => {
    try {
      const response = await fetch(`${import.meta.env.VITE_API_URL}/api/items`);
      if (response.ok) {
        const data = await response.json();
        setItems(data);
      }
    } catch (error) {
      console.error('Failed to connect to backend:', error);
    }
  };

  useEffect(() => {
    fetchItems();
  }, []);

  const handleItemDelete = (itemId: string) => {
    setItems((prev) => prev.filter((item) => item.id !== itemId));
  };

  const handleEditRequest = (item: ScannedItem) => {
    setEditingItem(item);
    setIsEditModalOpen(true);
  };

  return (
    <div className="bg-sky-950 min-h-screen text-center pb-10">
      <h2 className="text-5xl font-bold text-white p-7">BazaarScanner</h2>

      <button
        onClick={() => setIsScanModalOpen(true)}
        className="text-white px-6 py-3 bg-green-600 hover:bg-green-500 rounded-lg text-xl font-bold transition-colors shadow-lg"
      >
        Take a photo and add
      </button>

      {items.length === 0 ? (
        <p className="text-white mt-10">
          There's nothing here yet, try taking a picture!
        </p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 p-6 max-w-7xl mx-auto">
          {items.map((item) => (
            <ItemCard
              key={item.id}
              item={item}
              onItemDeleted={handleItemDelete}
              onEditRequest={handleEditRequest}
            />
          ))}
        </div>
      )}

      <ScanItemModal
        isOpen={isScanModalOpen}
        onClose={() => setIsScanModalOpen(false)}
        onSaveSuccess={fetchItems}
      />

      <EditItemModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        item={editingItem}
        onSaveSuccess={fetchItems}
      />
    </div>
  );
}

export default Home;
