import { CheckCircle2, XCircle, AlertTriangle } from 'lucide-react';
import { reconcileData } from '../data/mockData';

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-ZA', { style: 'currency', currency: 'ZAR' }).format(amount);
};

export default function Reconcile() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">Reconciliation</h1>
        <button className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition-colors">
          Auto-Match
        </button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs text-gray-500 uppercase bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="px-6 py-4">Internal System (A)</th>
                <th className="px-6 py-4">External Source (B)</th>
                <th className="px-6 py-4 text-center">Status</th>
                <th className="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {reconcileData.map((row) => (
                <tr key={row.id} className="hover:bg-gray-50/50">
                  <td className="px-6 py-4">
                    {row.systemA ? (
                      <div>
                        <div className="font-medium text-gray-900">{row.systemA.description}</div>
                        <div className="text-gray-500 mt-1">{row.systemA.reference} • {row.systemA.date}</div>
                        <div className="font-medium text-gray-900 mt-1">{formatCurrency(row.systemA.amount)}</div>
                      </div>
                    ) : (
                      <span className="text-gray-400 italic">No record found</span>
                    )}
                  </td>
                  <td className="px-6 py-4 border-l border-gray-100">
                    {row.systemB ? (
                      <div>
                        <div className="font-medium text-gray-900">{row.systemB.description}</div>
                        <div className="text-gray-500 mt-1">{row.systemB.reference} • {row.systemB.date}</div>
                        <div className="font-medium text-gray-900 mt-1">{formatCurrency(row.systemB.amount)}</div>
                      </div>
                    ) : (
                      <span className="text-gray-400 italic">No record found</span>
                    )}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      {row.status === 'matched' && (
                        <div className="flex items-center gap-1.5 text-green-600 bg-green-50 px-2.5 py-1 rounded-full text-xs font-medium">
                          <CheckCircle2 size={14} /> Matched
                        </div>
                      )}
                      {row.status === 'unmatched' && (
                        <div className="flex items-center gap-1.5 text-red-600 bg-red-50 px-2.5 py-1 rounded-full text-xs font-medium">
                          <XCircle size={14} /> Unmatched
                        </div>
                      )}
                      {row.status === 'discrepancy' && (
                        <div className="flex items-center gap-1.5 text-orange-600 bg-orange-50 px-2.5 py-1 rounded-full text-xs font-medium">
                          <AlertTriangle size={14} /> Discrepancy
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 text-right">
                    {row.status !== 'matched' && (
                      <button className="text-indigo-600 hover:text-indigo-900 font-medium text-sm">
                        Resolve
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
