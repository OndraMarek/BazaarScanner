import type { ScannedItem } from '../pages/Home';

interface ItemCardProps {
  item: ScannedItem;
  onItemDeleted: (id: string) => void;
  onEditRequest: (item: ScannedItem) => void;
}

function ItemCard({ item, onItemDeleted, onEditRequest }: ItemCardProps) {
  const handleDelete = async () => {
    if (!window.confirm('Really delete?')) return;
    try {
      const response = await fetch(
        `https://localhost:7102/api/items/${item.id}`,
        { method: 'DELETE' },
      );
      if (response.ok) onItemDeleted(item.id);
    } catch (error) {
      console.error('Failed to delete:', error);
    }
  };

  const imageUrl = item.imageUrl
    ? `https://localhost:7102${item.imageUrl}`
    : 'https://via.placeholder.com/300?text=No+photo';

  return (
    <div className="flex flex-col bg-sky-900 rounded-xl shadow-lg overflow-hidden border border-sky-700">
      <div className="w-full h-48 bg-sky-950">
        <img
          src={imageUrl}
          alt={item.name}
          className="w-full h-full object-cover"
        />
      </div>

      <div className="p-4 flex flex-col flex-grow text-white text-left">
        <h3 className="font-bold text-xl mb-2 line-clamp-2">{item.name}</h3>
        <p className="text-sky-300">
          Category: <span className="text-white">{item.type}</span>
        </p>
        <p className="text-sky-300">
          Number of items: <span className="text-white">{item.count}</span>
        </p>

        <div className="mt-auto pt-4 flex gap-2">
          <button
            onClick={() => onEditRequest(item)}
            className="flex-1 bg-yellow-600 hover:bg-yellow-500 text-white rounded py-2 transition-colors"
          >
            Edit
          </button>

          <button
            onClick={handleDelete}
            className="flex-1 bg-red-600 hover:bg-red-500 text-white rounded py-2 transition-colors"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}

export default ItemCard;
