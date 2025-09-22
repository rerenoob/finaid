

interface Deadline {
  id: string;
  title: string;
  dueDate: Date;
  type: 'fafsa' | 'document' | 'institution' | 'general';
  priority: 'high' | 'medium' | 'low';
  completed?: boolean;
}

interface DeadlineListProps {
  deadlines: Deadline[];
  onDeadlineClick?: (deadline: Deadline) => void;
  maxItems?: number;
}

export default function DeadlineList({ deadlines, onDeadlineClick, maxItems = 5 }: DeadlineListProps) {
  const getPriorityColor = (priority: Deadline['priority']) => {
    switch (priority) {
      case 'high': return 'bg-red-100 text-red-800 border-red-200';
      case 'medium': return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'low': return 'bg-green-100 text-green-800 border-green-200';
    }
  };

  const getTypeIcon = (type: Deadline['type']) => {
    switch (type) {
      case 'fafsa': return '📋';
      case 'document': return '📄';
      case 'institution': return '🏫';
      case 'general': return '📅';
    }
  };

  const formatDate = (date: Date): string => {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return 'Tomorrow';
    } else {
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }
  };

  const getDaysUntilDue = (date: Date): number => {
    const today = new Date();
    const diffTime = date.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  };

  const sortedDeadlines = deadlines
    .filter(d => !d.completed)
    .sort((a, b) => a.dueDate.getTime() - b.dueDate.getTime())
    .slice(0, maxItems);

  if (sortedDeadlines.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        <svg className="mx-auto h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <p className="text-lg font-medium">No upcoming deadlines</p>
        <p className="text-sm">You're all caught up!</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {sortedDeadlines.map((deadline) => {
        const daysUntilDue = getDaysUntilDue(deadline.dueDate);
        const isUrgent = daysUntilDue <= 3;
        
        return (
          <div
            key={deadline.id}
            onClick={() => onDeadlineClick?.(deadline)}
            className={`flex items-center justify-between p-3 bg-white border rounded-lg cursor-pointer hover:shadow-md transition-shadow ${
              isUrgent ? 'border-red-200 bg-red-50' : 'border-gray-200'
            }`}
          >
            <div className="flex items-center space-x-3 flex-1">
              <div className="flex-shrink-0 text-xl">
                {getTypeIcon(deadline.type)}
              </div>
              
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-gray-900 truncate">{deadline.title}</p>
                <div className="flex items-center space-x-2 mt-1">
                  <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getPriorityColor(deadline.priority)}`}>
                    {deadline.priority}
                  </span>
                  <span className="text-xs text-gray-500">
                    {formatDate(deadline.dueDate)}
                  </span>
                </div>
              </div>
            </div>
            
            <div className="flex items-center space-x-2">
              {isUrgent && (
                <div className="w-2 h-2 bg-red-500 rounded-full animate-pulse"></div>
              )}
              <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </div>
        );
      })}
    </div>
  );
}