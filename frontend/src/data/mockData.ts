export const kpiData = {
  totalRevenue: 1250000,
  unreconciledAmount: 45000,
  matchRate: 96.5,
  pendingTransactions: 124,
};

export const pieChartData = [
  { name: 'Matched', value: 1205000, color: '#4f46e5' }, // indigo-600
  { name: 'Unmatched', value: 45000, color: '#ef4444' }, // red-500
];

export const activityFeed = [
  { id: 1, action: 'Auto-matched 45 transactions from Standard Bank', time: '10 mins ago', type: 'success' },
  { id: 2, action: 'Manual override required for Invoice #4092', time: '1 hour ago', type: 'warning' },
  { id: 3, action: 'Daily export to Xero completed', time: '3 hours ago', type: 'info' },
  { id: 4, action: 'PayFast daily settlement synced', time: '5 hours ago', type: 'success' },
];

export const reconcileData = [
  {
    id: '1',
    systemA: { date: '2023-10-25', description: 'Payment #1024', amount: 1500.00, reference: 'INV-1024' },
    systemB: { date: '2023-10-25', description: 'Stripe Settlement', amount: 1500.00, reference: 'STR-9821' },
    status: 'matched',
  },
  {
    id: '2',
    systemA: { date: '2023-10-25', description: 'Payment #1025', amount: 450.50, reference: 'INV-1025' },
    systemB: null,
    status: 'unmatched',
  },
  {
    id: '3',
    systemA: { date: '2023-10-26', description: 'Refund #1011', amount: -200.00, reference: 'REF-1011' },
    systemB: { date: '2023-10-27', description: 'PayFast Refund', amount: -200.00, reference: 'PF-4421' },
    status: 'matched',
  },
  {
    id: '4',
    systemA: { date: '2023-10-26', description: 'Payment #1026', amount: 8900.00, reference: 'INV-1026' },
    systemB: { date: '2023-10-26', description: 'EFT Payment', amount: 8850.00, reference: 'EFT-INV1026' },
    status: 'discrepancy',
  },
  {
    id: '5',
    systemA: null,
    systemB: { date: '2023-10-26', description: 'Unknown Deposit', amount: 500.00, reference: 'DEP-UNK' },
    status: 'unmatched',
  },
];

export const sourcesData = [
  { id: '1', name: 'Standard Bank', type: 'Bank', lastSync: '10 mins ago', status: 'connected', records: 1420 },
  { id: '2', name: 'PayFast', type: 'Payment Gateway', lastSync: '1 hour ago', status: 'connected', records: 845 },
  { id: '3', name: 'Xero', type: 'Accounting', lastSync: '3 hours ago', status: 'connected', records: 2150 },
  { id: '4', name: 'Stripe', type: 'Payment Gateway', lastSync: '2 days ago', status: 'error', records: 412 },
];

export const exportPreviewData = [
  { date: '2023-10-25', account: 'Sales', debit: 0, credit: 1500.00, description: 'Matched Payment #1024' },
  { date: '2023-10-25', account: 'Bank', debit: 1500.00, credit: 0, description: 'Matched Payment #1024' },
  { date: '2023-10-26', account: 'Refunds', debit: 200.00, credit: 0, description: 'Matched Refund #1011' },
  { date: '2023-10-27', account: 'Bank', debit: 0, credit: 200.00, description: 'Matched Refund #1011' },
];
