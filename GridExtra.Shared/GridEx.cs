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
    public class AreaDefinition
    {
        public string Name { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }

        public AreaDefinition(string name, int column, int row, int columnSpan, int rowSpan)
        {
            this.Name = name;
            this.Column = column;
            this.Row = row;
            this.ColumnSpan = columnSpan;
            this.RowSpan = rowSpan;
        }
    }

    public static class GridEx
    {

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

            if (d == null || param == null)
            {
                return;
            }

            grid.ColumnDefinitions.Clear();

            var list = param.Split(',')
                            .Select(o => o.Trim());

            foreach (var item in list)
            {
                var gridLength = StringToGridLength(item);
                var columnDefinition = new ColumnDefinition() { Width = gridLength };
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

            if (d == null || param == null)
            {
                return;
            }

            grid.RowDefinitions.Clear();

            var list = param.Split(',')
                            .Select(o => o.Trim());

            foreach (var item in list)
            {
                var gridLength = StringToGridLength(item);
                var rowDefinition = new RowDefinition() { Height = gridLength };
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
            var columns = param.Split('\n')
                               .Select(o => o.Trim())
                               .Where(o => !string.IsNullOrWhiteSpace(o))
                               .Select(o => o.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            // 行×列数のチェック
            var num = columns.First().Count();
            var isValidRowColumn = columns.All(o => o.Count() == num);
            if (!isValidRowColumn)
            {
                // Invalid Row Columns...
            }


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

                if (isValid)
                {
                    result.Add(new AreaDefinition(group.Key, left, top, right - left + 1, bottom - top + 1));
                }
            }

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

            UpdateArea(ctrl, name);
        }

        private static void UpdateArea(FrameworkElement element, string name)
        {
            var grid = element.Parent as Grid;
            var areaList = GetAreaDefinitions(grid);
            if (areaList == null) return;

            var area = areaList.First(o => o.Name == name);
            if (area != null)
            {
                Grid.SetColumn(element, area.Column);
                Grid.SetRow(element, area.Row);
                Grid.SetColumnSpan(element, area.ColumnSpan);
                Grid.SetRowSpan(element, area.RowSpan);
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

            // Column, Row, ColumnSpan, RowSpan
            if (list.Count() != 4)
            {
                return;
            }

            Grid.SetColumn(ctrl, list[0]);
            Grid.SetRow(ctrl, list[1]);
            Grid.SetColumnSpan(ctrl, list[2]);
            Grid.SetRowSpan(ctrl, list[3]);
        }
    }
}
