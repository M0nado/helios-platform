using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace MonadoBlade.GUI.Performance
{
    /// <summary>
    /// Virtual scrolling data grid for handling large datasets (250 LOC)
    /// Implements virtual scrolling to render only visible items,
    /// dramatically improving performance with large collections.
    /// </summary>
    public class DataGridVirtualizer
    {
        private List<object> _allItems = new();
        private ObservableCollection<object> _visibleItems = new();
        private int _itemsPerPage = 50;
        private int _currentPageIndex = 0;
        private DataGrid _dataGrid;

        public event EventHandler<VirtualizationEventArgs> PageChanged;
        public event EventHandler<VirtualizationEventArgs> ItemsUpdated;

        public DataGridVirtualizer(DataGrid dataGrid, int itemsPerPage = 50)
        {
            _dataGrid = dataGrid;
            _itemsPerPage = itemsPerPage;
            _dataGrid.ItemsSource = _visibleItems;
            
            // Subscribe to scroll changes
            var scrollViewer = GetScrollViewer(_dataGrid);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        /// <summary>
        /// Set all items and refresh virtual view
        /// </summary>
        public void SetItems(IEnumerable<object> items)
        {
            _allItems = items?.ToList() ?? new List<object>();
            _currentPageIndex = 0;
            RefreshPage();
            ItemsUpdated?.Invoke(this, new VirtualizationEventArgs 
            { 
                TotalItems = _allItems.Count,
                PageIndex = _currentPageIndex 
            });
        }

        /// <summary>
        /// Add items to the collection
        /// </summary>
        public void AddItems(IEnumerable<object> items)
        {
            _allItems.AddRange(items ?? Enumerable.Empty<object>());
            RefreshPage();
            ItemsUpdated?.Invoke(this, new VirtualizationEventArgs 
            { 
                TotalItems = _allItems.Count,
                PageIndex = _currentPageIndex 
            });
        }

        /// <summary>
        /// Remove item from collection
        /// </summary>
        public void RemoveItem(object item)
        {
            _allItems.Remove(item);
            RefreshPage();
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        public void Clear()
        {
            _allItems.Clear();
            _visibleItems.Clear();
            _currentPageIndex = 0;
        }

        /// <summary>
        /// Get total item count
        /// </summary>
        public int GetTotalItemCount() => _allItems.Count;

        /// <summary>
        /// Get total page count
        /// </summary>
        public int GetTotalPageCount()
        {
            return _allItems.Count > 0 
                ? (int)Math.Ceiling((double)_allItems.Count / _itemsPerPage)
                : 0;
        }

        /// <summary>
        /// Go to specific page
        /// </summary>
        public void GoToPage(int pageIndex)
        {
            var maxPage = GetTotalPageCount() - 1;
            _currentPageIndex = Math.Max(0, Math.Min(pageIndex, maxPage));
            RefreshPage();
            PageChanged?.Invoke(this, new VirtualizationEventArgs 
            { 
                PageIndex = _currentPageIndex,
                TotalItems = _allItems.Count 
            });
        }

        /// <summary>
        /// Go to next page
        /// </summary>
        public void NextPage()
        {
            GoToPage(_currentPageIndex + 1);
        }

        /// <summary>
        /// Go to previous page
        /// </summary>
        public void PreviousPage()
        {
            GoToPage(_currentPageIndex - 1);
        }

        /// <summary>
        /// Set items per page (for virtual scrolling)
        /// </summary>
        public void SetItemsPerPage(int itemsPerPage)
        {
            if (itemsPerPage > 0)
            {
                _itemsPerPage = itemsPerPage;
                RefreshPage();
            }
        }

        /// <summary>
        /// Search items
        /// </summary>
        public List<object> Search(Func<object, bool> predicate)
        {
            return _allItems.Where(predicate).ToList();
        }

        /// <summary>
        /// Filter items
        /// </summary>
        public void Filter(Func<object, bool> predicate)
        {
            var filtered = _allItems.Where(predicate).ToList();
            _allItems = filtered;
            _currentPageIndex = 0;
            RefreshPage();
            ItemsUpdated?.Invoke(this, new VirtualizationEventArgs 
            { 
                TotalItems = _allItems.Count,
                PageIndex = _currentPageIndex 
            });
        }

        /// <summary>
        /// Sort items
        /// </summary>
        public void Sort<T>(Func<object, T> keySelector, bool descending = false)
        {
            _allItems = descending
                ? _allItems.OrderByDescending(keySelector).ToList()
                : _allItems.OrderBy(keySelector).ToList();
            _currentPageIndex = 0;
            RefreshPage();
        }

        /// <summary>
        /// Get current visible items
        /// </summary>
        public IEnumerable<object> GetVisibleItems() => _visibleItems;

        /// <summary>
        /// Get statistics
        /// </summary>
        public VirtualizationStatistics GetStatistics()
        {
            return new VirtualizationStatistics
            {
                TotalItems = _allItems.Count,
                VisibleItems = _visibleItems.Count,
                CurrentPage = _currentPageIndex,
                TotalPages = GetTotalPageCount(),
                ItemsPerPage = _itemsPerPage
            };
        }

        /// <summary>
        /// Handle scroll changes for dynamic loading
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            var scrollPercentage = scrollViewer.VerticalOffset / 
                (scrollViewer.ExtentHeight - scrollViewer.ViewportHeight);

            // Load next page when scrolling beyond 70%
            if (scrollPercentage > 0.7 && _currentPageIndex < GetTotalPageCount() - 1)
            {
                NextPage();
            }
        }

        /// <summary>
        /// Refresh current page display
        /// </summary>
        private void RefreshPage()
        {
            _visibleItems.Clear();

            var startIndex = _currentPageIndex * _itemsPerPage;
            var endIndex = Math.Min(startIndex + _itemsPerPage, _allItems.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                _visibleItems.Add(_allItems[i]);
            }
        }

        /// <summary>
        /// Get ScrollViewer from DataGrid
        /// </summary>
        private ScrollViewer GetScrollViewer(DependencyObject obj)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;

                var scrollViewerChild = GetScrollViewer(child);
                if (scrollViewerChild != null)
                    return scrollViewerChild;
            }
            return null;
        }
    }

    /// <summary>
    /// Virtualization statistics
    /// </summary>
    public class VirtualizationStatistics
    {
        public int TotalItems { get; set; }
        public int VisibleItems { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int ItemsPerPage { get; set; }

        public double RenderingEfficiency => 
            TotalItems > 0 ? (double)VisibleItems / TotalItems * 100 : 100;
    }

    /// <summary>
    /// Virtualization event arguments
    /// </summary>
    public class VirtualizationEventArgs : EventArgs
    {
        public int PageIndex { get; set; }
        public int TotalItems { get; set; }
    }
}
