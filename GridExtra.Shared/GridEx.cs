using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

#if WINDOWS_WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#elif WINDOWS_UWP
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
#endif


namespace SourceChord.GridExtra
{
#if WINDOWS_WPF
    using LayoutUpdateEventHandler = EventHandler;
#elif WINDOWS_UWP
    using LayoutUpdateEventHandler = EventHandler<object>;
#else
#endif

    public class AreaDefinition
    {
        public string Name { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; }
        public int ColumnSpan { get; set; }

        public AreaDefinition(string name, int row, int column, int rowSpan, int columnSpan)
        {
            this.Name = name;
            this.Row = row;
            this.Column = column;
            this.RowSpan = rowSpan;
            this.ColumnSpan = columnSpan;
        }
    }

    struct GridLengthDefinition
    {
        public GridLength GridLength;
        public double? Min;
        public double? Max;
    }

    public static class GridEx
    {
        public static Orientation GetAutoFillOrientation(DependencyObject obj)
        {
            return (Orientation)obj.GetValue(AutoFillOrientationProperty);
        }
        public static void SetAutoFillOrientation(DependencyObject obj, Orientation value)
        {
            obj.SetValue(AutoFillOrientationProperty, value);
        }
        // Using a DependencyProperty as the backing store for AutoFillOrientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoFillOrientationProperty =
            DependencyProperty.RegisterAttached("AutoFillOrientation", typeof(Orientation), typeof(GridEx), new PropertyMetadata(Orientation.Horizontal));


        public static bool GetAutoFillChildren(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoFillChildrenProperty);
        }
        public static void SetAutoFillChildren(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoFillChildrenProperty, value);
        }
        // Using a DependencyProperty as the backing store for AutoFillChildren.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoFillChildrenProperty =
            DependencyProperty.RegisterAttached("AutoFillChildren", typeof(bool), typeof(GridEx), new PropertyMetadata(false, OnAutoFillChildrenChanged));

        private static void OnAutoFillChildrenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            var isEnabled = (bool)e.NewValue;
            if (grid == null) { return; }

            if (isEnabled)
            {
                var layoutUpdateCallback = CreateLayoutUpdateHandler(grid);
                // イベントの登録
                grid.LayoutUpdated += layoutUpdateCallback;
                SetLayoutUpdatedCallback(grid, layoutUpdateCallback);

                // AutoFill処理を行う
                AutoFill(grid);
            }
            else
            {
                // イベントの解除
                var callback = GetLayoutUpdatedCallback(grid);
                grid.LayoutUpdated -= callback;

                // AutoFill処理のリセット
                ClearAutoFill(grid);
            }
        }


        private static LayoutUpdateEventHandler CreateLayoutUpdateHandler(Grid grid)
        {
            var prevCount = 0;
            var prevColumn = grid.ColumnDefinitions.Count;
            var prevRow = grid.RowDefinitions.Count;
            var prevOrientation = GetAutoFillOrientation(grid);

            var layoutUpdateCallback = new LayoutUpdateEventHandler((sender, args) =>
            {
                var count = grid.Children.Count;
                var column = grid.ColumnDefinitions.Count;
                var row = grid.RowDefinitions.Count;
                var orientation = GetAutoFillOrientation(grid);

                if (count != prevCount ||
                    column != prevColumn ||
                    row != prevRow ||
                    orientation != prevOrientation)
                {
                    AutoFill(grid);
                    prevCount = count;
                    prevColumn = column;
                    prevRow = row;
                    prevOrientation = orientation;
                }
            });

            return layoutUpdateCallback;
        }

        public static LayoutUpdateEventHandler GetLayoutUpdatedCallback(DependencyObject obj)
        {
            return (LayoutUpdateEventHandler)obj.GetValue(LayoutUpdatedCallbackProperty);
        }
        private static void SetLayoutUpdatedCallback(DependencyObject obj, LayoutUpdateEventHandler value)
        {
            obj.SetValue(LayoutUpdatedCallbackProperty, value);
        }
        // Using a DependencyProperty as the backing store for LayoutUpdatedCallback.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutUpdatedCallbackProperty =
            DependencyProperty.RegisterAttached("LayoutUpdatedCallback", typeof(LayoutUpdateEventHandler), typeof(GridEx), new PropertyMetadata(null));


        private static void AutoFill(Grid grid)
        {
            System.Diagnostics.Debug.WriteLine("AutoFill");
            var rowCount = grid.RowDefinitions.Count;
            var columnCount = grid.ColumnDefinitions.Count;
            var orientation = GetAutoFillOrientation(grid);

            var x = 0;
            var y = 0;
            // Gridの子要素を、順番にGrid内に並べていく
            foreach (FrameworkElement child in grid.Children)
            {
                // Visibility.Collapsedの項目は除外する
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                Grid.SetRow(child, y);
                Grid.SetColumn(child, x);
                Grid.SetRowSpan(child, 1);
                Grid.SetColumnSpan(child, 1);

                // Orientationの方向に進める
                if (orientation == Orientation.Horizontal)
                {
                    x++;
                    if (x >= columnCount)
                    {
                        x = 0;
                        y++;
                    }
                }
                else
                {
                    y++;
                    if (y >= rowCount)
                    {
                        y = 0;
                        x++;
                    }
                }
            }
        }

        private static void ClearAutoFill(Grid grid)
        {
            foreach (FrameworkElement child in grid.Children)
            {
                child.ClearValue(Grid.RowProperty);
                child.ClearValue(Grid.ColumnProperty);
                child.ClearValue(Grid.RowSpanProperty);
                child.ClearValue(Grid.ColumnSpanProperty);
            }
        }


        public static string GetColumnDefinition(DependencyObject obj)
        {
            return (string)obj.GetValue(ColumnDefinitionProperty);
        }
        public static void SetColumnDefinition(DependencyObject obj, string value)
        {
            obj.SetValue(ColumnDefinitionProperty, value);
        }
        // Using a DependencyProperty as the backing store for ColumnDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColumnDefinitionProperty =
            DependencyProperty.RegisterAttached("ColumnDefinition", typeof(string), typeof(GridEx), new PropertyMetadata(null, OnColumnDefinitionChanged));

        private static void OnColumnDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            var param = e.NewValue as string;

            InitializeColumnDefinition(grid, param);
        }

        private static void InitializeColumnDefinition(Grid grid, string param)
        {
            if (grid == null || param == null)
            {
                return;
            }

            grid.ColumnDefinitions.Clear();

            var list = param.Split(',')
                            .Select(o => o.Trim());

            foreach (var item in list)
            {
                var def = StringToGridLengthDefinition(item);
                var columnDefinition = new ColumnDefinition() { Width = def.GridLength };
                if (def.Max != null) { columnDefinition.MaxWidth = def.Max.Value; }
                if (def.Min != null) { columnDefinition.MinWidth = def.Min.Value; }
                grid.ColumnDefinitions.Add(columnDefinition);
            }
        }

        public static string GetRowDefinition(DependencyObject obj)
        {
            return (string)obj.GetValue(RowDefinitionProperty);
        }
        public static void SetRowDefinition(DependencyObject obj, string value)
        {
            obj.SetValue(RowDefinitionProperty, value);
        }
        // Using a DependencyProperty as the backing store for RowDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RowDefinitionProperty =
            DependencyProperty.RegisterAttached("RowDefinition", typeof(string), typeof(GridEx), new PropertyMetadata(null, OnRowDefinitionChanged));

        private static void OnRowDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            var param = e.NewValue as string;

            InitializeRowDefinition(grid, param);
        }

        private static void InitializeRowDefinition(Grid grid, string param)
        {
            if (grid == null || param == null)
            {
                return;
            }

            grid.RowDefinitions.Clear();

            var list = param.Split(',')
                            .Select(o => o.Trim());

            foreach (var item in list)
            {
                var def = StringToGridLengthDefinition(item);
                var rowDefinition = new RowDefinition() { Height = def.GridLength };
                if (def.Max != null) { rowDefinition.MaxHeight = def.Max.Value; }
                if (def.Min != null) { rowDefinition.MinHeight = def.Min.Value; }
                grid.RowDefinitions.Add(rowDefinition);
            }
        }



        // ↓GridEx内部でだけ使用する、プライベートな添付プロパティ
        public static IList<AreaDefinition> GetAreaDefinitions(DependencyObject obj)
        {
            return (IList<AreaDefinition>)obj.GetValue(AreaDefinitionsProperty);
        }
        private static void SetAreaDefinitions(DependencyObject obj, IList<AreaDefinition> value)
        {
            obj.SetValue(AreaDefinitionsProperty, value);
        }
        // Using a DependencyProperty as the backing store for AreaDefinitions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreaDefinitionsProperty =
            DependencyProperty.RegisterAttached("AreaDefinitions", typeof(IList<AreaDefinition>), typeof(GridEx), new PropertyMetadata(null));



        public static string GetTemplateArea(DependencyObject obj)
        {
            return (string)obj.GetValue(TemplateAreaProperty);
        }
        public static void SetTemplateArea(DependencyObject obj, string value)
        {
            obj.SetValue(TemplateAreaProperty, value);
        }
        // Using a DependencyProperty as the backing store for TemplateArea.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplateAreaProperty =
            DependencyProperty.RegisterAttached("TemplateArea", typeof(string), typeof(GridEx), new PropertyMetadata(null, OnTemplateAreaChanged));

        private static void OnTemplateAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            var param = e.NewValue as string;

            if (d == null || param == null)
            {
                return;
            }

            // 行×列数のチェック
            // 空行や、スペースを除去して、行×列のデータ構造に変形
            var columns = param.Split(new[] { '\n', '/' })
                               .Select(o => o.Trim())
                               .Where(o => !string.IsNullOrWhiteSpace(o))
                               .Select(o => o.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            // 行×列数のチェック
            var num = columns.FirstOrDefault().Count();
            var isValidRowColumn = columns.All(o => o.Count() == num);
            if (!isValidRowColumn)
            {
                // Invalid Row Columns...
                throw new ArgumentException("Invalid Row/Column definition.");
            }

            // グリッドを一度初期化
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            // GridEx.RowDefinition/GridEx.ColumnDefinitionの設定内容で、行/列を初期化
            InitializeRowDefinition(grid, GetRowDefinition(grid));
            InitializeColumnDefinition(grid, GetColumnDefinition(grid));

            // グリッド数を調整(不足分の行/列を足す)
            var rowShortage = columns.Count() - grid.RowDefinitions.Count;
            for (var i = 0; i < rowShortage; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            var columnShortage = num - grid.ColumnDefinitions.Count;
            for (var i = 0; i < columnShortage; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Area定義をパース
            var areaList = ParseAreaDefinition(columns);
            SetAreaDefinitions(grid, areaList);

            // 全体レイアウトの定義が変わったので、
            // Gridの子要素のすべてのRegion設定を反映しなおす
            foreach(FrameworkElement child in grid.Children)
            {
                UpdateArea(child, GetAreaName(child));
            }
        }


        private static IList<AreaDefinition> ParseAreaDefinition(IEnumerable<string[]> columns)
        {
            var result = new List<AreaDefinition>();

            // Regionが正しく連結されているかチェック
            var flatten = columns.SelectMany(
                    (item, index) => item.Select((o, xIndex) => new { row = index, column = xIndex, name = o })
                );

            var groups = flatten.GroupBy(o => o.name);
            foreach (var group in groups)
            {
                var left = group.Min(o => o.column);
                var top = group.Min(o => o.row);
                var right = group.Max(o => o.column);
                var bottom = group.Max(o => o.row);

                var isValid = true;
                for (var y = top; y <= bottom; y++)
                    for (var x = left; x <= right; x++)
                    {
                        isValid = isValid && group.Any(o => o.column == x && o.row == y);
                    }

                if (!isValid)
                {
                    throw new ArgumentException($"\"{group.Key}\" is invalid area definition.");
                }

                result.Add(new AreaDefinition(group.Key, top, left, bottom - top + 1, right - left + 1));
            }

            return result;
        }

        private static GridLengthDefinition StringToGridLengthDefinition(string source)
        {
            var r = new System.Text.RegularExpressions.Regex(@"(^[^\(\)]+)(?:\((.*)-(.*)\))?");
            var m = r.Match(source);

            var length = m.Groups[1].Value;
            var min = m.Groups[2].Value;
            var max = m.Groups[3].Value;

            double temp;
            var result = new GridLengthDefinition()
            {
                GridLength = StringToGridLength(length),
                Min = double.TryParse(min, out temp) ? temp : (double?)null,
                Max = double.TryParse(max, out temp) ? temp : (double?)null
            };

            return result;
        }

#if WINDOWS_WPF
        private static GridLength StringToGridLength(string source)
        {
            var glc = TypeDescriptor.GetConverter(typeof(GridLength));
            return (GridLength)glc.ConvertFromString(source);
        }
#elif WINDOWS_UWP
        private static GridLength StringToGridLength(string source)
        {
            GridLength gridLength;
            if (source.ToLower() == "auto")
            {
                gridLength = new GridLength(0.0, GridUnitType.Auto);
            }
            else
            {
                var r = new System.Text.RegularExpressions.Regex(@"([\d\.]*)(\*?)");
                var m = r.Match(source);

                var val = m.Groups[1].Value;
                var unit = m.Groups[2].Value;

                double size;
                var isValid = double.TryParse(val, out size);

                if (unit == "*")
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        gridLength = new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        if (!isValid) { throw new ArgumentException(); }
                        gridLength = new GridLength(size, GridUnitType.Star);
                    }
                }
                else if (string.IsNullOrEmpty(unit))
                {
                    if (!isValid) { throw new ArgumentException(); ; }
                    gridLength = new GridLength(size, GridUnitType.Pixel);
                }
                else
                {
                    // 変換失敗
                    throw new ArgumentException();
                }
            }

            return gridLength;
        }
#else
#endif


        //=====================================================================
        // Grid内の子要素に適用するための添付プロパティ類
        //=====================================================================
        public static string GetAreaName(DependencyObject obj)
        {
            return (string)obj.GetValue(AreaNameProperty);
        }
        public static void SetAreaName(DependencyObject obj, string value)
        {
            obj.SetValue(AreaNameProperty, value);
        }
        // Using a DependencyProperty as the backing store for AreaName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreaNameProperty =
            DependencyProperty.RegisterAttached("AreaName", typeof(string), typeof(GridEx), new PropertyMetadata(null, OnAreaNameChanged));

        private static void OnAreaNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as FrameworkElement;
            var name = e.NewValue as string;

            if (ctrl == null || name == null)
            {
                return;
            }

            if (ctrl.Parent == null)
            {
                ctrl.Loaded += (_, __) => { UpdateArea(ctrl, name); };
            }
            else
            {
                UpdateArea(ctrl, name);
            }
        }

        private static void UpdateArea(FrameworkElement element, string name)
        {
            var grid = element.Parent as Grid;
            if (grid == null) return;
            var areaList = GetAreaDefinitions(grid);
            if (areaList == null) return;

            var area = areaList.FirstOrDefault(o => o.Name == name);
            if (area != null)
            {
                Grid.SetRow(element, area.Row);
                Grid.SetColumn(element, area.Column);
                Grid.SetRowSpan(element, area.RowSpan);
                Grid.SetColumnSpan(element, area.ColumnSpan);
            }
        }


        public static string GetArea(DependencyObject obj)
        {
            return (string)obj.GetValue(AreaProperty);
        }
        public static void SetArea(DependencyObject obj, string value)
        {
            obj.SetValue(AreaProperty, value);
        }
        // Using a DependencyProperty as the backing store for Area.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreaProperty =
            DependencyProperty.RegisterAttached("Area", typeof(string), typeof(GridEx), new PropertyMetadata(null, OnAreaChanged));

        private static void OnAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as FrameworkElement;
            var param = e.NewValue as string;

            if (d == null || param == null)
            {
                return;
            }

            var list = param.Split(',')
                            .Select(o => o.Trim())
                            .Select(o => int.Parse(o))
                            .ToList();

            // Row, Column, RowSpan, ColumnSpan
            if (list.Count() != 4)
            {
                return;
            }

            Grid.SetRow(ctrl, list[0]);
            Grid.SetColumn(ctrl, list[1]);
            Grid.SetRowSpan(ctrl, list[2]);
            Grid.SetColumnSpan(ctrl, list[3]);
        }
    }
}
