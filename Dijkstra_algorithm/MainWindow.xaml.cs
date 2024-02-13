using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using Dijkstra_algorithm.Model;

namespace Dijkstra_algorithm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const double _diameter = 50;//диаметр визуализации вершины
        private const double _edgeLabelSize = 20;//размер прямоугольного модуля в котором указан вес ребра

        private const int _fontSize = 20;//размер шрифта на визуальных вершинах
        private const int _edgeFontSize = 15;//размер шрифта на визуальных ребрах

        private int _count;
        private bool _findMinDistance;
        private bool _findAllMinDistanse;

        private List<Node> _cloud;
        private ReachableNodeList _reachableNodes;

        private List<Node> _nodes;//массив вершин
        private List<Edge> _edges;//массив ребер

        private Node _edgeNode1, _edgeNode2;
        private SolidColorBrush _unvisitedBrush, _visitedBrush;
        private bool _isGraphConnected;
        private bool restart = false;

        private Node CurrentNode;

        public MainWindow()
        {
            InitializeComponent();
            drawingCanvas.SetValue(Canvas.ZIndexProperty, 0);

            _cloud = new List<Node>();
            _reachableNodes = new ReachableNodeList();

            _nodes = new List<Node>();
            _edges = new List<Edge>();

            _count = 1;
            _findMinDistance = false;
            _findAllMinDistanse = false;
            _isGraphConnected = true;

            _unvisitedBrush = new SolidColorBrush(Colors.Green);
            _visitedBrush = new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// Событие обрабатывающее GUI:
        ///     -создание вершин
        ///     -создание ребер
        ///     -рассчет минимального расстояния 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                Point clickPoint = e.GetPosition(drawingCanvas);

                if (HasClickedOnNode(clickPoint.X, clickPoint.Y))
                {
                    AssignEndNodes(clickPoint.X, clickPoint.Y);

                    if (_edgeNode1 != null && _edgeNode2 == null)
                    {
                        if (_findAllMinDistanse)
                        {
                            textcurrentnode.Text = CurrentNode.Label;

                            DataTable tbl = new DataTable("Nodes");
                            tbl.Columns.Add("Label", typeof(string));
                            tbl.Columns.Add("distance", typeof(double));

                            foreach (Node n in _nodes)
                            {
                                if (n.Label == CurrentNode.Label) continue;
                                FindMinDistancePath(_edgeNode1, n);
                                tbl.Rows.Add(n.Label, TotalCost(_edgeNode1, n));
                            }
                            nodesList.DataContext = tbl;
                        }
                    }

                    if (_edgeNode1 != null && _edgeNode2 != null)
                    {
                        if (_findMinDistance)
                        {
                            statusLabel.Content = "Calculating...";
                            LaunchMinDistanceTask();
                        }
                        else
                        {
                            //задаем вес ребра
                            double distance = GetEdgeDistance();
                            if (distance != 0.0)
                            {
                                Edge edge = CreateEdge(_edgeNode1, _edgeNode2, distance);
                                _edges.Add(edge);

                                _edgeNode1.Edges.Add(edge);
                                _edgeNode2.Edges.Add(edge);

                                PaintEdge(edge);
                            }
                            ClearEdgeNodes();
                        }
                    }
                }
                else
                {
                    if (!OverlapsNode(clickPoint))
                    {
                        Node n = CreateNode(clickPoint);
                        _nodes.Add(n);
                        PaintNode(n);
                        _count++;
                        ClearEdgeNodes();
                    }
                }
            }
        }

        private void LaunchMinDistanceTask()
        {
            //Task.Factory.StartNew(() =>
            FindMinDistancePath(_edgeNode1, _edgeNode2);
            //)
            //.ContinueWith((task) =>
            //{
            //    if (task.IsFaulted)
            //        MessageBox.Show("Произошла ошибка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            //    else
                    PaintMinDistancePath(_edgeNode1, _edgeNode2);

            //},
            //TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ClearEdgeNodes()
        {
            _edgeNode1 = _edgeNode2 = null;
        }

        /// <summary>
        /// Метод определяет нажал ли пользователь на узел
        /// используется для создания ребра или для указания конечных узлов, для которых нужно найти
        /// минимальное расстояние
        /// </summary>
        /// <param name="x">координата x</param>
        /// <param name="y">координата y</param>
        /// <returns>Нажат ли существующий узел</returns>
        private bool HasClickedOnNode(double x, double y)
        {
            bool rez = false;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].HasPoint(new Point(x, y)))
                {
                    rez = true;
                    break;
                }
            }
            return rez;
        }

        /// <summary>
        /// Получает узел с определенной координатой
        /// </summary>
        /// <param name="x">координата x</param>
        /// <param name="y">координата y</param>
        /// <returns>Узел, который был найден, или null, если в заданных координатах нет узла</returns>
        private Node GetNodeAt(double x, double y)
        {
            Node rez = null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].HasPoint(new Point(x, y)))
                {
                    rez = _nodes[i];
                    break;
                }
            }
            return rez;
        }
        /// <summary>
        /// После создания узла
        /// проверяет что он не перекрывает существующий узел
        /// </summary>
        /// <param name="p">A x,y точка</param>
        /// <returns>Пересекается ли узел с существующим узлом</returns>
        private bool OverlapsNode(Point p)
        {
            bool rez = false;
            double distance;
            for (int i = 0; i < _nodes.Count; i++)
            {
                distance = GetDistance(p, _nodes[i].Center);
                if (distance < _diameter)
                {
                    rez = true;
                    break;
                }
            }
            return rez;
        }

        /// <summary>
        /// Открывает диалоговое окно для ввода веса ребра указанного пользователем
        /// </summary>
        /// <returns>Значение веса ребра, заданного пользователем</returns>
        private double GetEdgeDistance()
        {
            double distance = 0.0;
            DistanceDialog dd = new DistanceDialog();
            dd.Owner = this;

            dd.ShowDialog();
            distance = dd.Distance;

            return distance;
        }
        /// <summary>
        /// Вычисляет эвклидовое расстояние между двумя точками
        /// </summary>
        /// <param name="p1">Точка 1</param>
        /// <param name="p2">Точка 2</param>
        /// <returns>Расстояние между двумя точками</returns>
        private double GetDistance(Point p1, Point p2)
        {
            double xSq = Math.Pow(p1.X - p2.X, 2);
            double ySq = Math.Pow(p1.Y - p2.Y, 2);
            double dist = Math.Sqrt(xSq + ySq);

            return dist;
        }

        private void AssignEndNodes(double x, double y)
        {
            CurrentNode = GetNodeAt(x, y);
            if (CurrentNode != null)
            {
                if (_edgeNode1 == null)
                {
                    _edgeNode1 = CurrentNode;

                    statusLabel.Content = "Вы выбрали узел источник № " + CurrentNode.Label + ". Пожалуйста выберите целевой узел или используйте функцию расчета минимальных расстояний до всех узлов";
                }
                else
                {
                    if (CurrentNode != _edgeNode1)
                    {
                        _edgeNode2 = CurrentNode;
                        statusLabel.Content = "Введите вес ребра, соединяющего заданные вершины";
                    }
                }
            }
        }

        /// <summary>
        /// Создает новый узел, используя координаты, заданной точки
        /// </summary>
        /// <param name="p">Объект Point, который содержит координаты для создания узла</param>
        /// <returns></returns>
        private Node CreateNode(Point p)
        {
            double nodeCenterX = p.X - _diameter / 2;
            double nodeCenterY = p.Y - _diameter / 2;
            Node newNode = new Node(new Point(nodeCenterX, nodeCenterY), p, _count.ToString(), _diameter);
            return newNode;
        }

        /// <summary>
        /// Рисует узел(точку) в окне
        /// </summary>
        /// <param name="node">Объект Node с заданными координатами</param>
        private void PaintNode(Node node)
        {
            //Координаты узла
            Ellipse ellipse = new Ellipse();
            if (node.Visited)
                ellipse.Fill = _visitedBrush;
            else
                ellipse.Fill = _unvisitedBrush;

            ellipse.Width = _diameter;
            ellipse.Height = _diameter;

            ellipse.SetValue(Canvas.LeftProperty, node.Location.X);
            ellipse.SetValue(Canvas.TopProperty, node.Location.Y);
            ellipse.SetValue(Canvas.ZIndexProperty, 2);
            //Добавление и в окно данного узла
            drawingCanvas.Children.Add(ellipse);

            //Задаем номер узла
            TextBlock tb = new TextBlock();
            tb.Text = node.Label;
            tb.Foreground = Brushes.White;
            tb.FontSize = _fontSize;
            tb.TextAlignment = TextAlignment.Center;
            tb.SetValue(Canvas.LeftProperty, node.Center.X - (_fontSize / 3.5 * node.Label.Length));
            tb.SetValue(Canvas.TopProperty, node.Center.Y - _fontSize / 1.5);
            tb.SetValue(Canvas.ZIndexProperty, 3);

            //Добавляем номер в узел для отображения
            drawingCanvas.Children.Add(tb);
        }

        private Edge CreateEdge(Node node1, Node node2, double distance)
        {
            return new Edge(node1, node2, distance);
        }

        private void PaintEdge(Edge edge)
        {
            //рисуем ребро
            Line line = new Line();
            line.X1 = edge.FirstNode.Center.X;
            line.X2 = edge.SecondNode.Center.X;

            line.Y1 = edge.FirstNode.Center.Y;
            line.Y2 = edge.SecondNode.Center.Y;

            if (edge.Visited)
                line.Stroke = _visitedBrush;
            else
                line.Stroke = new SolidColorBrush(Colors.Black);

            line.StrokeThickness = 3;
            line.SetValue(Canvas.ZIndexProperty, 1);
            drawingCanvas.Children.Add(line);

            //рисуем текстовый блок с отображением заданного веса ребра
            Point edgeLabelPoint = GetEdgeLabelCoordinate(edge);
            TextBlock tb = new TextBlock();
            tb.Text = edge.Length.ToString();
            tb.Foreground = Brushes.White;

            if (edge.Visited)
                tb.Background = _visitedBrush;
            else
                tb.Background = new SolidColorBrush(Colors.Black);

            tb.Padding = new Thickness(5);
            tb.FontSize = _edgeFontSize;

            tb.MinWidth = _edgeLabelSize;
            tb.MinHeight = _edgeLabelSize;

            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tb.TextAlignment = TextAlignment.Center;

            tb.SetValue(Canvas.LeftProperty, edgeLabelPoint.X);
            tb.SetValue(Canvas.TopProperty, edgeLabelPoint.Y);
            tb.SetValue(Canvas.ZIndexProperty, 2);
            drawingCanvas.Children.Add(tb);
        }

        /// <summary>
        /// Вычисляет координаты, где должен быть нарисован текстовый блок с весом ребра
        /// </summary>
        private Point GetEdgeLabelCoordinate(Edge edge)
        {

            double x = Math.Abs(edge.FirstNode.Location.X - edge.SecondNode.Location.X) / 2;
            double y = Math.Abs(edge.FirstNode.Location.Y - edge.SecondNode.Location.Y) / 2;

            if (edge.FirstNode.Location.X > edge.SecondNode.Location.X)
                x += edge.SecondNode.Location.X;
            else
                x += edge.FirstNode.Location.X;

            if (edge.FirstNode.Location.Y > edge.SecondNode.Location.Y)
                y += edge.SecondNode.Location.Y;
            else
                y += edge.FirstNode.Location.Y;

            return new Point(x, y);
        }
        /// <summary>
        /// Реализация алгоритма Алгори́тм Дейкстры
        /// </summary>
        /// <param name="start">Вершина источник</param>
        /// <param name="end">Целевая вершина</param>
        private void FindMinDistancePath(Node start, Node end)
        {
            _cloud.Clear();
            _reachableNodes.Clear();
            Node currentNode = start;
            currentNode.Visited = true;
            start.TotalCost = 0;
            _cloud.Add(currentNode);
            ReachableNode currentReachableNode;

            while (currentNode != end)
            {
                AddReachableNodes(currentNode);
                UpdateReachableNodesTotalCost(currentNode);

                //если мы не можем достичь другого узла - график не связан
                if (_reachableNodes.ReachableNodes.Count == 0)
                {
                    _isGraphConnected = false;
                    break;
                }

                //получаем ближайший доступный узел
                currentReachableNode = _reachableNodes.ReachableNodes[0];
                //удаляем из списка доступных узлов данный узел
                _reachableNodes.RemoveReachableNode(currentReachableNode);
                //отмечаем текущий узел, как просмотренный
                currentNode.Visited = true;
                //присваиваем текущему узлу следующий ближайший узел
                currentNode = currentReachableNode.Node;
                //устанавливаем указатель на ребро, связывающего текущий узел с предыдущим
                currentNode.EdgeCameFrom = currentReachableNode.Edge;
                //отмечаем данное ребро как просмотренное
                currentReachableNode.Edge.Visited = true;

                _cloud.Add(currentNode);
            }
        }

        /// <summary>
        /// Рисует путь минимального расстояния начиная с целевой вершины
        /// </summary>
        /// <param name="start">Вершина источник</param>
        /// <param name="end">Целевая вершина</param>
        private void PaintMinDistancePath(Node start, Node end)
        {
            if (_isGraphConnected)
            {
                Node currentNode = end;
                double totalCost = 0;
                while (currentNode != start)
                {
                    currentNode.Visited = true;
                    currentNode.EdgeCameFrom.Visited = true;
                    totalCost += currentNode.EdgeCameFrom.Length;

                    PaintNode(currentNode);
                    PaintEdge(currentNode.EdgeCameFrom);

                    currentNode = GetNeighbour(currentNode, currentNode.EdgeCameFrom);
                }
                //рисует текущий узел, как узел источник
                if (currentNode != null)
                    PaintNode(currentNode);

                start.Visited = true;
                statusLabel.Content = "Минимальное расстоние от узла №" + start.Label + " до узла №" + end.Label + ": " + totalCost.ToString();
            }
            else
            {
                ClearEdgeNodes();
                _isGraphConnected = true;
                _findMinDistance = false;
                statusLabel.Content = "Нажмите по рабочей области, чтобы создать новый узел";
                MessageBox.Show("Граф не связан ребрами, не удается найти связь между узлами", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        private double TotalCost(Node start, Node end)
        {
            Node currentNode = end;
            double totalCost = 0;
            while (currentNode != start)
            {
                //currentNode.Visited = true;
                //currentNode.EdgeCameFrom.Visited = true;
                if (_isGraphConnected)
                    totalCost += currentNode.EdgeCameFrom.Length;
                else
                {
                    MessageBox.Show("Граф не связан ребрами, не удается найти связь между узлами", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);

                }

                //PaintNode(currentNode);
                //PaintEdge(currentNode.EdgeCameFrom);

                currentNode = GetNeighbour(currentNode, currentNode.EdgeCameFrom);
            }
            return totalCost;
        }

        /// <summary>
        /// На каждой итерации обнаруживает доступные узлы, просматривает все ребра, которые исходят от узла
        /// </summary>
        /// <param name="node"></param>
        private void AddReachableNodes(Node node)
        {
            Node neighbour;
            ReachableNode rn;
            foreach (Edge edge in node.Edges)
            {
                neighbour = GetNeighbour(node, edge);
                //проверяем, что предыдущий узел задан верно
                if (node.EdgeCameFrom == null || neighbour != GetNeighbour(node, node.EdgeCameFrom))
                {
                    //проверяем наличие данного узла в массиве всех узлов
                    if (!_cloud.Contains(neighbour))
                    {
                        if (_reachableNodes.HasNode(neighbour))
                        {
                            if (node.TotalCost + edge.Length < neighbour.TotalCost)
                            {
                                rn = _reachableNodes.GetReachableNodeFromNode(neighbour);
                                rn.Edge = edge;
                            }
                        }
                        else
                        {
                            rn = new ReachableNode(neighbour, edge);
                            _reachableNodes.AddReachableNode(rn);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Пересчитывает при каждой итерации общий вес ребер, в связи с которыми имеются достижимые узлы
        /// </summary>
        /// <param name="node">Текущий узел</param>
        private void UpdateReachableNodesTotalCost(Node node)
        {
            double currentCost = node.TotalCost;
            foreach (ReachableNode rn in _reachableNodes.ReachableNodes)
            {
                if (currentCost + rn.Edge.Length < rn.Node.TotalCost || rn.Node.TotalCost == -1)
                    rn.Node.TotalCost = currentCost + rn.Edge.Length;
            }

            _reachableNodes.SortReachableNodes();
        }

        /// <summary>
        /// Находит вершину на другой стороне ребра
        /// </summary>
        /// <param name="node">Узел от которого ищем пути к соседним узлам</param>
        /// <param name="edge">Текущее ребро</param>
        /// <returns></returns>
        private Node GetNeighbour(Node node, Edge edge)
        {
            if (edge.FirstNode == node)
                return edge.SecondNode;
            else
                return edge.FirstNode;
        }

        private void findMinDistanceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (restart)
            {
                Restart();
                PaintAllNodes();
                PaintAllEdges();
                nodesList.DataContext = null;
            }
            this._findMinDistance = true;
            statusLabel.Content = "Выберите узел источник и целевой узел для поиска минимального расстояния между ними";
            restart = true;
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            textcurrentnode.Text = "не выбран";

            for(int i=0; i < drawingCanvas.Children.Count; i++)
            {
                if (!((UIElement)drawingCanvas.Children[i] is Button))
                {
                    drawingCanvas.Children.RemoveAt(i);
                    i--;
                }
            }
        }

        private void Clear()
        {
            this._nodes.Clear();
            this._edges.Clear();
            this._cloud.Clear();
            this._reachableNodes.Clear();
            this._findMinDistance = false;
            this._count = 1;
        }

        private void Restart()
        {
            this._findMinDistance = false;
            _findAllMinDistanse = false;
            _edgeNode1 = _edgeNode2 = null;
            _count = 1;

            this._cloud.Clear();
            this._reachableNodes.Clear();

            foreach (Node n in _nodes)
                n.Reset();
            //n.Visited = false;

            foreach (Edge e in _edges)
                e.Reset();
                    //e.Visited = false;
        }

        private void PaintAllNodes()
        {
            foreach (Node n in _nodes)
                PaintNode(n);
        }

        private void allMinDistanceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (restart)
            {
                Restart();
                PaintAllNodes();
                PaintAllEdges();
                nodesList.DataContext = null;
            }
            _findAllMinDistanse = true;
            statusLabel.Content = "Выберите узел для отображения минимальных расстояний к остальным узлам";
            restart = true;
        }

        private void PaintAllEdges()
        {
            foreach (Edge e in _edges)
                PaintEdge(e);
        }

        private void restartBtn_Click(object sender, RoutedEventArgs e)
        {
            Restart();
            PaintAllNodes();
            PaintAllEdges();
            nodesList.DataContext = null;
        }
    }
}
