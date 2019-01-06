#region Usings

using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using DameGUI.Data;
using LogicLib;

#endregion Usings

namespace DameGUI.Components
{
    /// <summary> Interaction logic for Renderer.xaml </summary>
    public partial class Renderer : UserControl, IDisposable
    {
        private static SolidColorBrush beigeSqBrush = new SolidColorBrush(Colors.Beige);
        private static Color DarkWhite = Color.FromRgb(255 - 30, 255 - 30, 255 - 30);
        private static SolidColorBrush greenSqBrush = new SolidColorBrush(Colors.DarkGreen);
        private static SolidColorBrush hoverSqBrush = new SolidColorBrush(Colors.CornflowerBlue);
        private static Color LightBlack = Color.FromRgb(70, 70, 70);
        private static MeshGeometry3D protoCone = new ConeGeometry() { Center = new Point3D(0, 0, 0.5), Rtop = 3.5, Rbottom = 3.5, Height = 1 }.Mesh3D;
        private static MeshGeometry3D protoCube = new CubeGeometry() { Origin = new Point3D(0, 0, 0), Width = 4, Depth = 4, Height = 1 }.Mesh3D;
        private static MeshGeometry3D protoDame = new DameGeometry() { Origin = new Point3D(0, 0, 0), Width = 3, Depth = 3, Height = 1 }.Mesh3D;
        private static SolidColorBrush redSqBrush = new SolidColorBrush(Colors.Red);
        private static SolidColorBrush selctSqBrush = new SolidColorBrush(Colors.LightCoral);
        private static SolidColorBrush yellowSqBrush = new SolidColorBrush(Colors.Yellow);
        private bool callbackBlink = false;

        private int selectedVisualindex = -1;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        public event EventHandler<EventArgs> Blinked;

        public event EventHandler<SquareSelectedArgs> SelectedSquare;

        private enum CamPositions
        { Black, ToBlack, White, ToWhite }

        private enum PieceTypes
        { None, Stone, Chute, Dame }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Fields

        private Vector3DAnimation animLokBlack = new Vector3DAnimation(new Vector3D(0, -32, -20), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Vector3DAnimation animLokToBlack = new Vector3DAnimation(new Vector3D(-32, 0, -20), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Vector3DAnimation animLoktoWhite = new Vector3DAnimation(new Vector3D(32, 0, -20), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Vector3DAnimation animLokWhite = new Vector3DAnimation(new Vector3D(0, 32, -20), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Point3DAnimation animPosBlack = new Point3DAnimation(new Point3D(32, 124, 50), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Point3DAnimation animPosToBlack = new Point3DAnimation(new Point3D(124, 32, 50), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Point3DAnimation animPosToWhite = new Point3DAnimation(new Point3D(-60, 32, 50), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Point3DAnimation animPosWhite = new Point3DAnimation(new Point3D(32, -60, 50), new System.Windows.Duration(new TimeSpan(0, 0, 0, 0, 1100)));
        private Dictionary<int, ModelVisual3D> boardSquares = new Dictionary<int, ModelVisual3D>();
        private CamPositions camPos = CamPositions.White;
        private int lastIndex = -1;

        //int timerSrc = -1;
        //int timerTrg = -1;
        private List<KeyValuePair<Lcs, ModelVisual3D>> stones = new List<KeyValuePair<Lcs, ModelVisual3D>>();

        private PointLight svetlo = new PointLight(Colors.White, new Point3D(0, 0, 10));
        private Timer timer = new Timer(400);
        private int timerCnt = 0;
        private List<int> timerSrcs = new List<int>();
        private AnimationClock whitePos, toWhitePos, blackPos, toBlackPos;

        #endregion Fields

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor + dispose

        public Renderer()
        {
            InitializeComponent();

            timer.Elapsed += timer_Elapsed;

            this.AddBoard(svetlo);
            //--------------------------------------------------------------------------------------

            #region viewport mousedown

            viewport.MouseDown += (src, arg) =>
            {
                HitTestResult result = VisualTreeHelper.HitTest(viewport, arg.GetPosition(viewport));
                if (result != null && result.VisualHit is ModelVisual3D)
                {
                    ModelVisual3D visual = (ModelVisual3D)result.VisualHit;

                    int boardIndex = -1;

                    Lcs? stoneIdx = MatchStone(visual);
                    if (stoneIdx.HasValue)
                    {
                        boardIndex = (int)stoneIdx.Value;
                    }

                    if (boardIndex == -1)
                    {
                        boardIndex = MatchVisual(visual);
                    }
                    else
                    {
                        visual = boardSquares[boardIndex];
                    }

                    if (selectedVisualindex > -1)
                    {
                        int row = selectedVisualindex / 10;
                        int column = selectedVisualindex % 10;

                        int rowM = row % 2;
                        int colM = column % 2;

                        Color clr = Colors.Beige;

                        if (rowM == 1 && colM == 1 || rowM == 0 && colM == 0) { clr = Colors.DarkGreen; }

                        ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[selectedVisualindex].Content).Children)[1]).Material
                            = new DiffuseMaterial(new SolidColorBrush(clr));

                        selectedVisualindex = -1;
                    }

                    if (stoneIdx.HasValue)
                    {
                        if (SelectedSquare != null) { SelectedSquare(this, new SquareSelectedArgs(stoneIdx.Value)); }
                    }
                    else if (boardIndex > -1)
                    {
                        try
                        {
                            if (Enum.IsDefined(typeof(Lcs), boardIndex))
                            {
                                Lcs dsrc = (Lcs)boardIndex;
                                if (SelectedSquare != null) { SelectedSquare(this, new SquareSelectedArgs(dsrc)); }
                            }
                        }
                        catch { }
                    }
                }
            };

            #endregion viewport mousedown

            //--------------------------------------------------------------------------------------

            #region viewport mousemove

            viewport.MouseMove += (src, arg) =>
            {
                HitTestResult result = VisualTreeHelper.HitTest(viewport, arg.GetPosition(viewport));
                if (result != null && result.VisualHit is ModelVisual3D)
                {
                    ModelVisual3D visual = (ModelVisual3D)result.VisualHit;

                    int boardIndex = -1;

                    Lcs? stoneIdx = MatchStone(visual);
                    if (stoneIdx.HasValue)
                    {
                        boardIndex = (int)stoneIdx.Value;
                    }

                    if (boardIndex == -1)
                    {
                        boardIndex = MatchVisual(visual);
                    }
                    else
                    {
                        visual = boardSquares[boardIndex];
                    }

                    if (boardIndex > -1)
                    {
                        if (lastIndex != boardIndex)
                        {
                            if (lastIndex > -1)
                            {
                                int row = lastIndex / 10;
                                int column = lastIndex % 10;

                                int rowM = row % 2;
                                int colM = column % 2;

                                SolidColorBrush brush = beigeSqBrush;

                                if (lastIndex == selectedVisualindex) { brush = selctSqBrush; }
                                else if (rowM == 1 && colM == 1 || rowM == 0 && colM == 0) { brush = greenSqBrush; }

                                ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[lastIndex].Content).Children)[1]).Material
                                    = new DiffuseMaterial(brush);
                            }
                            lastIndex = boardIndex;
                            ((GeometryModel3D)((Model3DCollection)((Model3DGroup)visual.Content).Children)[1]).Material
                                = new DiffuseMaterial(hoverSqBrush);
                        }
                        return;
                    }
                }
            };

            #endregion viewport mousemove
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (timer != null) { timer.Dispose(); }
        }

        #endregion Constructor + dispose

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region animace rotace

        public void Animate(bool toWhite)
        {
            if (toWhite && camPos == CamPositions.Black)
            {
                toWhitePos = animPosToWhite.CreateClock();
                toWhitePos.Completed += toWhitePos_Completed;

                cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, toWhitePos);
                cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLoktoWhite.CreateClock());
            }
            else if (toWhite && camPos == CamPositions.ToWhite)
            {
                whitePos = animPosWhite.CreateClock();
                whitePos.Completed += whitePos_Completed;

                cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, whitePos);
                cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLokWhite.CreateClock());
            }
            else if (!toWhite && camPos == CamPositions.White)
            {
                toBlackPos = animPosToBlack.CreateClock();
                toBlackPos.Completed += toBlackPos_Completed;

                cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, toBlackPos);
                cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLokToBlack.CreateClock());
            }
            else if (!toWhite && camPos == CamPositions.ToBlack)
            {
                blackPos = animPosBlack.CreateClock();
                blackPos.Completed += blackPos_Completed;

                cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, blackPos);
                cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLokBlack.CreateClock());
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void blackPos_Completed(object sender, EventArgs e)
        {
            camPos = CamPositions.Black;
            blackPos.Completed -= blackPos_Completed;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void toBlackPos_Completed(object sender, EventArgs e)
        {
            camPos = CamPositions.ToBlack;
            toBlackPos.Completed -= toBlackPos_Completed;

            blackPos = animPosBlack.CreateClock();
            blackPos.Completed += blackPos_Completed;

            cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, blackPos);
            cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLokBlack.CreateClock());
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void toWhitePos_Completed(object sender, EventArgs e)
        {
            camPos = CamPositions.ToWhite;
            toWhitePos.Completed -= toWhitePos_Completed;

            whitePos = animPosWhite.CreateClock();
            whitePos.Completed += whitePos_Completed;

            cam.ApplyAnimationClock(PerspectiveCamera.PositionProperty, whitePos);
            cam.ApplyAnimationClock(PerspectiveCamera.LookDirectionProperty, animLokWhite.CreateClock());
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void whitePos_Completed(object sender, EventArgs e)
        {
            camPos = CamPositions.White;
            whitePos.Completed -= whitePos_Completed;
        }

        #endregion animace rotace

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region timer_Elapsed

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (timerCnt % 2 == 0)
            {
                foreach (int i in timerSrcs)
                {
                    if (i > -1)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[i].Content).Children)[1]).Material
                                = new DiffuseMaterial(redSqBrush);
                        }));
                    }
                }
            }
            else
            {
                foreach (int i in timerSrcs)
                {
                    if (i > -1)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[i].Content).Children)[1]).Material
                                = new DiffuseMaterial(yellowSqBrush);
                        }));
                    }
                }
            }

            if (timerCnt > 5)
            {
                timer.Stop();

                int row, column, rowM, colM;
                row = column = rowM = colM = -1;
                SolidColorBrush brush = beigeSqBrush;

                foreach (int i in timerSrcs)
                {
                    if (selectedVisualindex == i)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[i].Content).Children)[1]).Material
                                = new DiffuseMaterial(selctSqBrush);
                        }));
                    }
                    else
                    {
                        row = i / 10;
                        column = i % 10;

                        rowM = row % 2;
                        colM = column % 2;

                        if (rowM == 1 && colM == 1 || rowM == 0 && colM == 0) { brush = greenSqBrush; }

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[i].Content).Children)[1]).Material
                                = new DiffuseMaterial(brush);
                        }));
                    }
                    /*
                    row = timerTrg / 10;
                    column = timerTrg % 10;

                    rowM = row % 2;
                    colM = column % 2;

                    brush = beigeSqBrush;
                    if (rowM == 1 && colM == 1 || rowM == 0 && colM == 0) { brush = greenSqBrush; }

                    this.Dispatcher.Invoke((Action)(() => {
                        ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[timerTrg].Content).Children)[1]).Material
                            = new DiffuseMaterial(brush);
                    }));*/
                }
                timerSrcs.Clear();

                if (Blinked != null) { Blinked(this, new EventArgs()); }
                callbackBlink = false;
            }
            timerCnt++;
        }

        #endregion timer_Elapsed

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region blink - spusti naznaceni tahu

        public void Blink(bool callback, params Lcs[] parameters)
        {
            timerCnt = 0;
            callbackBlink = callback;

            foreach (Lcs lcs in parameters)
            {
                try
                {
                    timerSrcs.Add((int)lcs);
                }
                catch { }
            }

            timer.Start();
        }

        #endregion blink - spusti naznaceni tahu

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MovePiece

        public void MovePiece(Lcs origin, Lcs next)
        {
            KeyValuePair<Lcs, ModelVisual3D>? modelO = MatchStone(origin);
            if (!modelO.HasValue) { return; }
            KeyValuePair<Lcs, ModelVisual3D>? modelN = MatchStone(next);
            if (modelN.HasValue) { return; }

            stones.Remove(modelO.Value);

            int location = (int)next;

            int nextR = location / 10;
            int nextC = location - (nextR * 10);

            switch (GetPieceType(modelO.Value.Value))
            {
                case PieceTypes.Stone:
                    modelO.Value.Value.Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4/* bottom edge */, 0);
                    break;

                case PieceTypes.Chute:
                    modelO.Value.Value.Transform = new TranslateTransform3D(nextC * 8 + 2 /* left edge */, nextR * 8 + 2/* bottom edge */, 0);
                    break;

                case PieceTypes.Dame:
                    modelO.Value.Value.Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4/* bottom edge */, 0);
                    break;
            }

            stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(next, modelO.Value.Value));
        }

        #endregion MovePiece

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region AddPiece

        public void AddPiece(Lcs location, Stones type)
        {
            KeyValuePair<Lcs, ModelVisual3D>? modelO = MatchStone(location);
            if (modelO.HasValue) { return; }

            int coords = (int)location;

            int nextR = coords / 10;
            int nextC = coords - (nextR * 10);

            switch (type)
            {
                case Stones.WhiteStone:

                    #region WhiteStone

                    ModelVisual3D wSt = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                                Geometry = ((ConeGeometry)FindResource("coneGeo")).Mesh3D
                                            }
                                        }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(wSt);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, wSt));

                    #endregion WhiteStone

                    break;

                case Stones.BlackStone:

                    #region BlackStone

                    ModelVisual3D bSt = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                                Geometry = ((ConeGeometry)FindResource("coneGeo")).Mesh3D
                                            }
                                        }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(bSt);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, bSt));

                    #endregion BlackStone

                    break;

                case Stones.WhiteParachute:

                    #region WhiteParachute

                    ModelVisual3D wPar = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                                Geometry = ((CubeGeometry)FindResource("cubeGeo")).Mesh3D
                                            }
                                        }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(wPar);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, wPar));

                    #endregion WhiteParachute

                    break;

                case Stones.BlackParachute:

                    #region BlackParachute

                    ModelVisual3D bPar = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                                Geometry = ((CubeGeometry)FindResource("cubeGeo")).Mesh3D
                                            }
                                        }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(bPar);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, bPar));

                    #endregion BlackParachute

                    break;

                case Stones.WhiteDame:

                    #region WhiteDame

                    ModelVisual3D wD = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                svetlo,
                                new GeometryModel3D() {
                                    Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                    Geometry = ((DameGeometry)FindResource("dameGeo")).Mesh3D
                                }
                            }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(wD);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, wD));

                    #endregion WhiteDame

                    break;

                case Stones.BlackDame:

                    #region BlackDame

                    ModelVisual3D bD = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                svetlo,
                                new GeometryModel3D() {
                                    Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                    Geometry = ((DameGeometry)FindResource("dameGeo")).Mesh3D
                                }
                            }
                        },
                        Transform = new TranslateTransform3D(nextC * 8 + 4 /* left edge */, nextR * 8 + 4 /* bottom edge */, 0)
                    };
                    viewport.Children.Add(bD);
                    stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(location, bD));

                    #endregion BlackDame

                    break;
            }
        }

        #endregion AddPiece

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region TakePiece

        public void TakePiece(Lcs location)
        {
            KeyValuePair<Lcs, ModelVisual3D>? modelO = MatchStone(location);
            if (!modelO.HasValue) { return; }

            stones.Remove(modelO.Value);

            viewport.Children.Remove(modelO.Value.Value);
        }

        #endregion TakePiece

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region ChangePiece

        public void ChangePiece(Game.PieceChangedEventArgs args)
        {
            KeyValuePair<Lcs, ModelVisual3D>? modelO = MatchStone(args.Location);
            if (!modelO.HasValue) { return; }

            Model3DGroup group = modelO.Value.Value.Content as Model3DGroup;
            GeometryModel3D geoModel = group.Children[1] as GeometryModel3D;

            int location = (int)args.Location;

            int nextR = location / 10;
            int nextC = location - (nextR * 10);

            switch (args.NewType)
            {
                case Stones.BlackStone:
                case Stones.WhiteStone:
                    geoModel.Geometry = new ConeGeometry() { Center = new Point3D(0, 0, 0.5), Rtop = 3.5, Rbottom = 3.5, Height = 1 }.Mesh3D;
                    break;

                case Stones.WhiteDame:
                case Stones.BlackDame:
                    geoModel.Geometry = new DameGeometry() { Origin = new Point3D(0, 0, 0), Width = 3, Depth = 3, Height = 1 }.Mesh3D;
                    break;

                case Stones.BlackParachute:
                case Stones.WhiteParachute:
                    geoModel.Geometry = new CubeGeometry() { Origin = new Point3D(0, 0, 0), Width = 3, Depth = 3, Height = 1 }.Mesh3D;
                    break;
            }
        }

        #endregion ChangePiece

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Init

        public void Init(Dictionary<Lcs, Stones> locations)
        {
            for (int i = 70; i > -1; i -= 10)
            {
                for (int j = 0; j < 8; j++)
                {
                    int target = i + j;
                    Lcs abc = (Lcs)Enum.ToObject(typeof(Lcs), target);

                    if (locations.ContainsKey(abc))
                    {
                        Stones stone = locations[abc];
                        switch (stone)
                        {
                            case Stones.WhiteStone:

                                #region WhiteStone

                                ModelVisual3D wSt = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                                Geometry = ((ConeGeometry)FindResource("coneGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i / 10 * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(wSt);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, wSt));
                                break;

                            #endregion WhiteStone

                            case Stones.BlackStone:

                                #region BlackStone

                                ModelVisual3D bSt = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                                Geometry = ((ConeGeometry)FindResource("coneGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i / 10 * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(bSt);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, bSt));
                                break;

                            #endregion BlackStone

                            case Stones.WhiteParachute:

                                #region WhiteParachute

                                ModelVisual3D wPar = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                                //Geometry = (MeshGeometry3D)FindResource("paraGeometry2")
                                                Geometry = ((CubeGeometry)FindResource("cubeGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i / 10 * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(wPar);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, wPar));
                                break;

                            #endregion WhiteParachute

                            case Stones.BlackParachute:

                                #region BlackParachute

                                ModelVisual3D bPar = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                                Geometry = ((CubeGeometry)FindResource("cubeGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i / 10 * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(bPar);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, bPar));
                                break;

                            #endregion BlackParachute

                            case Stones.WhiteDame:

                                #region WhiteDame

                                ModelVisual3D wD = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.DarkWhite)),
                                                Geometry = ((DameGeometry)FindResource("dameGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(wD);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, wD));

                                #endregion WhiteDame

                                break;

                            case Stones.BlackDame:

                                #region BlackDame

                                ModelVisual3D bD = new ModelVisual3D()
                                {
                                    Content = new Model3DGroup()
                                    {
                                        Children = new Model3DCollection(2) {
                                            svetlo,
                                            new GeometryModel3D() {
                                                Material = new DiffuseMaterial(new SolidColorBrush(Renderer.LightBlack)),
                                                Geometry = ((DameGeometry)FindResource("dameGeo")).Mesh3D
                                            }
                                        }
                                    },
                                    Transform = new TranslateTransform3D(j * 8 + 4 /* left edge */, i * 8 + 4 /* bottom edge */, 0)
                                };
                                viewport.Children.Add(bD);
                                stones.Add(new KeyValuePair<Lcs, ModelVisual3D>(abc, bD));

                                #endregion BlackDame

                                break;
                        }
                    }
                }
            }

            this.InvalidateVisual();
        }

        #endregion Init

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Reset

        public void Reset(Dictionary<Lcs, Stones> locations)
        {
            foreach (KeyValuePair<Lcs, ModelVisual3D> stone in stones)
            {
                viewport.Children.Remove(stone.Value);
            }

            stones.Clear();

            this.Init(locations);
        }

        #endregion Reset

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region AddBoard

        private void AddBoard(PointLight svetlo)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    ModelVisual3D model = new ModelVisual3D()
                    {
                        Content = new Model3DGroup()
                        {
                            Children = new Model3DCollection(2) {
                                svetlo,
                                new GeometryModel3D() {
                                    Material = new DiffuseMaterial(new SolidColorBrush(i % 2 == 0 ? j % 2 == 0 ? Colors.DarkGreen /*.Wheat*/ : Colors.Beige /*Beige*/ : j % 2 == 0 ? Colors.Beige : Colors.DarkGreen /*.Wheat*/)),
                                    Geometry = (MeshGeometry3D)FindResource("boardSquareGeometry")
                                }
                            }
                        },
                        Transform = new TranslateTransform3D(j * 8 /* left edge */, i * 8 /* bottom edge */, 0)
                    };

                    boardSquares.Add(10 * i + j, model);
                    viewport.Children.Add(model);
                }
            }

            #region row labels

            for (int i = 1; i < 9; i++)
            {
                ModelVisual3D modelx = new ModelVisual3D()
                {
                    Content = new Model3DGroup()
                    {
                        Children = new Model3DCollection(2) {
                            svetlo,
                            new GeometryModel3D() {
                                Material = new DiffuseMaterial(
                                    new VisualBrush(
                                        new TextBlock(){
                                            Foreground = new SolidColorBrush(Colors.White),
                                            Text = i.ToString()
                                        })),
                                Geometry = (MeshGeometry3D)FindResource("rowsGeoB")
                            }
                        }
                    },
                    Transform = new TranslateTransform3D(64 /* cols * 8 / left edge */, (i - 1) * 8 /* bottom edge */, 0)
                };
                viewport.Children.Add(modelx);
            }

            for (int i = 1; i < 9; i++)
            {
                ModelVisual3D modelx = new ModelVisual3D()
                {
                    Content = new Model3DGroup()
                    {
                        Children = new Model3DCollection(2) {
                            svetlo,
                            new GeometryModel3D() {
                                Material = new DiffuseMaterial(
                                    new VisualBrush(
                                        new TextBlock(){
                                            Foreground = new SolidColorBrush(Colors.White),
                                            Text = i.ToString()
                                        })),
                                Geometry = (MeshGeometry3D)FindResource("rowsGeoW")
                            }
                        }
                    },
                    Transform = new TranslateTransform3D(-4 /* cols * 8 / left edge */, (i - 1) * 8 /* bottom edge */, 0)
                };
                viewport.Children.Add(modelx);
            }

            #endregion row labels

            #region column labels

            string[] pole = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };

            for (int i = 0; i < 8; i++)
            {
                ModelVisual3D modelx = new ModelVisual3D()
                {
                    Content = new Model3DGroup()
                    {
                        Children = new Model3DCollection(2) {
                            svetlo,
                            new GeometryModel3D() {
                                Material = new DiffuseMaterial(
                                    new VisualBrush(
                                        new TextBlock(){
                                            Foreground = new SolidColorBrush(Colors.White),
                                            Text = pole[i]
                                        })),
                                Geometry = (MeshGeometry3D)FindResource("colsGeoW")
                            }
                        }
                    },
                    Transform = new TranslateTransform3D(i * 8 /* left edge */, -4/*row * 4*/ /* bottom edge */, 0)
                };
                viewport.Children.Add(modelx);
            }

            for (int i = 0; i < 8; i++)
            {
                ModelVisual3D modelx = new ModelVisual3D()
                {
                    Content = new Model3DGroup()
                    {
                        Children = new Model3DCollection(2) {
                            svetlo,
                            new GeometryModel3D() {
                                Material = new DiffuseMaterial(
                                    new VisualBrush(
                                        new TextBlock(){
                                            Foreground = new SolidColorBrush(Colors.White),
                                            Text = pole[i]
                                        })),
                                Geometry = (MeshGeometry3D)FindResource("colsGeoB")
                            }
                        }
                    },
                    Transform = new TranslateTransform3D(i * 8 /* left edge */, 64/*row * 4*/ /* bottom edge */, 0)
                };
                viewport.Children.Add(modelx);
            }
        }

        #endregion column labels

        #endregion AddBoard

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GetPieceType + CompareMeshes

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool CompareMeshes(Geometry3D a, MeshGeometry3D b)
        {
            if (a.Bounds.X == b.Bounds.X &&
               a.Bounds.Y == b.Bounds.Y &&
               a.Bounds.Z == b.Bounds.Z &&
               a.Bounds.SizeX == b.Bounds.SizeX &&
               a.Bounds.SizeY == b.Bounds.SizeY &&
               a.Bounds.SizeZ == b.Bounds.SizeZ) { return true; }
            return false;
        }

        private PieceTypes GetPieceType(ModelVisual3D model)
        {
            Model3DGroup group = model.Content as Model3DGroup;
            GeometryModel3D geoModel = group.Children[1] as GeometryModel3D;

            if (CompareMeshes(geoModel.Geometry, protoCube)) { return PieceTypes.Chute; }
            else if (CompareMeshes(geoModel.Geometry, protoCone)) { return PieceTypes.Stone; }
            else if (CompareMeshes(geoModel.Geometry, protoDame)) { return PieceTypes.Dame; }

            return PieceTypes.None;
        }

        #endregion GetPieceType + CompareMeshes

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MatchVisual

        private Lcs? MatchStone(ModelVisual3D hit)
        {
            foreach (KeyValuePair<Lcs, ModelVisual3D> pair in stones) { if (pair.Value == hit) { return pair.Key; } }
            return null;
        }

        private KeyValuePair<Lcs, ModelVisual3D>? MatchStone(Lcs origin)
        {
            foreach (KeyValuePair<Lcs, ModelVisual3D> pair in stones) { if (pair.Key == origin) { return pair; } }
            return null;
        }

        private int MatchVisual(ModelVisual3D hit)
        {
            foreach (KeyValuePair<int, ModelVisual3D> pair in boardSquares) { if (pair.Value == hit) { return pair.Key; } }
            return -1;
        }

        #endregion MatchVisual

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region SelectedSquare event + SquareSelectedArgs class

        public class SquareSelectedArgs : EventArgs
        {
            public SquareSelectedArgs(Lcs src)
            {
                this.Source = src;
            }

            public Lcs Source { get; set; }
        }

        #endregion SelectedSquare event + SquareSelectedArgs class

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region SelectSquare

        public void SelectSquare(Lcs loc, bool trigger)
        {
            try
            {
                selectedVisualindex = (int)loc;

                ((GeometryModel3D)((Model3DCollection)((Model3DGroup)boardSquares[selectedVisualindex].Content).Children)[1]).Material
                                = new DiffuseMaterial(selctSqBrush);

                if (SelectedSquare != null && trigger) { SelectedSquare(this, new SquareSelectedArgs(loc)); }
            }
            catch { }
        }

        #endregion SelectSquare
    }
}