using Godot;
using System;

public partial class Player : Node2D {

    // This function is made by me, the comments aren't because of chatgpt.
    public override void _PhysicsProcess(double delta) {
        RigidBody2D RigidBody = GetNode<RigidBody2D>("RigidBody2D");
        Camera2D Camera = GetNode<Camera2D>("/root/Game/Camera2D");

        // Constantly moves the player to the right and makes the camera follow
        RigidBody.LinearVelocity = new Vector2(800, RigidBody.LinearVelocity.Y);
        Camera.Position = new Vector2(RigidBody.Position.X + 960, Camera.Position.Y);

        if (Input.IsActionJustPressed("Jump") && IsOnGround(RigidBody)) {
            RigidBody.ApplyImpulse(Vector2.Up * 1000, Vector2.Zero);
        }

        // Kills the player if they hit a wall
        CheckForWall(RigidBody);

        if (IsOnGround(RigidBody)) {
            RigidBody.RotationDegrees = 0;
        } else {
            RigidBody.RotationDegrees += 3.5f;
        }
    }

    // Chatgpt made this function since I couldn't be bothered to figure this out
    public void CheckForWall(RigidBody2D RigidBody) {
        Vector2 rayOrigin = RigidBody.GlobalPosition;
        Vector2 rayEnd = rayOrigin + new Vector2(70, 0);

        var spaceState = GetWorld2D().DirectSpaceState;

        var query = new PhysicsRayQueryParameters2D();
        query.From = rayOrigin;
        query.To = rayEnd;
        query.Exclude = new Godot.Collections.Array<Rid> { RigidBody.GetRid() };

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0) {
            Die();
        }
    }

    // Just a clone of CheckForWall but checks downwards instead
    public bool IsOnGround(RigidBody2D RigidBody) {
        Vector2 rayOrigin = RigidBody.GlobalPosition;
        Vector2 rayEnd = rayOrigin + new Vector2(00, 80);

        var spaceState = GetWorld2D().DirectSpaceState;

        var query = new PhysicsRayQueryParameters2D();
        query.From = rayOrigin;
        query.To = rayEnd;
        query.Exclude = new Godot.Collections.Array<Rid> { RigidBody.GetRid() };

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0) {
            return true;
        } else {
            return false;
        }
    }

    public void Die() {
        GetTree().ReloadCurrentScene();
    }
}
