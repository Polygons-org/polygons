using Godot;

public partial class Editor : Node2D {
    int CameraSpeed = 10;
    private bool _isDragging = false;
    private Vector2 _lastMousePos;

    public override void _Ready() {
        GetNode<Sprite2D>($"Background").Modulate = new Color(Globals.BackGroundRed / 255f, Globals.BackGroundGreen / 255f, Globals.BackGroundBlue / 255f);

        for (int i = 0; i <= 8; i++) {
            GetNode<Sprite2D>($"Ground/Ground{i}").Modulate = new Color(Globals.GroundRed / 255f, Globals.GroundGreen / 255f, Globals.GroundBlue / 255f);
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Middle) {
            _isDragging = mouseBtn.Pressed;
            if (_isDragging)
                _lastMousePos = GetViewport().GetMousePosition();
        }
    }

    public override void _Process(double delta) {
        if (_isDragging) {
            Vector2 currentMouse = GetViewport().GetMousePosition();
            Vector2 diff = _lastMousePos - currentMouse; // inverted so dragging right moves right
            _lastMousePos = currentMouse;

            if (diff != Vector2.Zero) {
                GetNode<Camera2D>("Camera2D").Position += diff;
                GetNode<ColorRect>("StartLine").Position += new Vector2(0, diff.Y);
                GetNode<Sprite2D>("Background").Position += diff;
                GetNode<Node2D>("Ground").Position += new Vector2(diff.X, 0);
            }
        }
    }
}
