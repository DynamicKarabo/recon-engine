import { RefreshCw, Database, CreditCard, Building2, AlertCircle, CheckCircle2 } from 'lucide-react';
import { sourcesData } from '../data/mockData';

const getIcon = (type: string) => {
  switch (type) {
    case 'Bank': return <Building2 size={24} className="text-blue-600" />;
    case 'Payment Gateway': return <CreditCard size={24} className="text-purple-600" />;
    case 'Accounting': return <Database size={24} className="text-emerald-600" />;
    default: return <Database size={24} className="text-gray-600" />;
  }
};

export default function Sources() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">Data Sources</h1>
        <button className="flex items-center gap-2 bg-white border border-gray-200 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors shadow-sm">
          <RefreshCw size={16} />
          Sync All
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {sourcesData.map((source) => (
          <div key={source.id} className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <div className="flex justify-between items-start mb-4">
              <div className="flex items-center gap-4">
                <div className="p-3 bg-gray-50 rounded-lg">
                  {getIcon(source.type)}
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">{source.name}</h3>
                  <p className="text-sm text-gray-500">{source.type}</p>
                </div>
              </div>
              <button className="text-gray-400 hover:text-gray-600">
                <RefreshCw size={18} />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-4 py-4 border-t border-gray-50">
              <div>
                <p className="text-xs text-gray-500 mb-1">Status</p>
                <div className="flex items-center gap-1.5">
                  {source.status === 'connected' ? (
                    <><CheckCircle2 size={14} className="text-green-500" /> <span className="text-sm font-medium text-gray-900">Connected</span></>
                  ) : (
                    <><AlertCircle size={14} className="text-red-500" /> <span className="text-sm font-medium text-gray-900">Error</span></>
                  )}
                </div>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-1">Last Sync</p>
                <p className="text-sm font-medium text-gray-900">{source.lastSync}</p>
              </div>
            </div>

            <div className="pt-4 border-t border-gray-50 flex justify-between items-center">
              <p className="text-sm text-gray-500">
                <span className="font-medium text-gray-900">{source.records.toLocaleString()}</span> records synced
              </p>
              <button className="text-indigo-600 hover:text-indigo-900 text-sm font-medium">
                Configure
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
