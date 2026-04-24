import { Download, FileText, Settings } from 'lucide-react';
import { exportPreviewData } from '../data/mockData';

const formatCurrency = (amount: number) => {
  if (amount === 0) return '-';
  return new Intl.NumberFormat('en-ZA', { style: 'currency', currency: 'ZAR' }).format(amount);
};

export default function Export() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">Export to Accounting</h1>
        <button className="flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition-colors">
          <Download size={16} />
          Export Now
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Controls */}
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 lg:col-span-1 space-y-6 h-fit">
          <div className="flex items-center gap-2 mb-2">
            <Settings className="text-gray-400" size={20} />
            <h2 className="text-lg font-semibold text-gray-900">Export Settings</h2>
          </div>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Target System</label>
              <select className="w-full bg-gray-50 border border-gray-200 text-gray-900 text-sm rounded-lg focus:ring-indigo-500 focus:border-indigo-500 block p-2.5">
                <option>Xero</option>
                <option>QuickBooks</option>
                <option>Sage</option>
                <option>CSV (Generic)</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Date Range</label>
              <select className="w-full bg-gray-50 border border-gray-200 text-gray-900 text-sm rounded-lg focus:ring-indigo-500 focus:border-indigo-500 block p-2.5">
                <option>Today</option>
                <option>Yesterday</option>
                <option>Last 7 Days</option>
                <option>This Month</option>
                <option>Custom Range...</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Data Type</label>
              <div className="space-y-2 mt-2">
                <label className="flex items-center">
                  <input type="checkbox" className="w-4 h-4 text-indigo-600 bg-gray-100 border-gray-300 rounded focus:ring-indigo-500" defaultChecked />
                  <span className="ml-2 text-sm text-gray-700">Matched Transactions</span>
                </label>
                <label className="flex items-center">
                  <input type="checkbox" className="w-4 h-4 text-indigo-600 bg-gray-100 border-gray-300 rounded focus:ring-indigo-500" />
                  <span className="ml-2 text-sm text-gray-700">Unmatched (As Draft)</span>
                </label>
                <label className="flex items-center">
                  <input type="checkbox" className="w-4 h-4 text-indigo-600 bg-gray-100 border-gray-300 rounded focus:ring-indigo-500" defaultChecked />
                  <span className="ml-2 text-sm text-gray-700">Journal Entries</span>
                </label>
              </div>
            </div>
          </div>
        </div>

        {/* Preview */}
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 lg:col-span-2">
          <div className="flex items-center gap-2 mb-6">
            <FileText className="text-gray-400" size={20} />
            <h2 className="text-lg font-semibold text-gray-900">Journal Entry Preview</h2>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="text-xs text-gray-500 uppercase bg-gray-50 border-b border-gray-100">
                <tr>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Account</th>
                  <th className="px-4 py-3">Description</th>
                  <th className="px-4 py-3 text-right">Debit</th>
                  <th className="px-4 py-3 text-right">Credit</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {exportPreviewData.map((row, index) => (
                  <tr key={index} className="hover:bg-gray-50/50">
                    <td className="px-4 py-3 text-gray-500">{row.date}</td>
                    <td className="px-4 py-3 font-medium text-gray-900">{row.account}</td>
                    <td className="px-4 py-3 text-gray-600">{row.description}</td>
                    <td className="px-4 py-3 text-right text-gray-900">{formatCurrency(row.debit)}</td>
                    <td className="px-4 py-3 text-right text-gray-900">{formatCurrency(row.credit)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-50 font-medium">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-right text-gray-900">Total</td>
                  <td className="px-4 py-3 text-right text-gray-900">{formatCurrency(1700)}</td>
                  <td className="px-4 py-3 text-right text-gray-900">{formatCurrency(1700)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
          <p className="text-xs text-gray-500 mt-4 text-center">Showing preview of next export batch. 4 of 124 records.</p>
        </div>
      </div>
    </div>
  );
}
