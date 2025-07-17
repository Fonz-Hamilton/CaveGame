using Godot;
using System;

public partial class PlayerCharacter : CharacterBody2D
{
	
	[Export]
	public float Sprint = 150.0f;
	[Export]
    public float BaseSpeed = 75.0f;
    [Export]
	public float JumpVelocity = -400.0f;
    [Export]
    public float Speed = 75.0f;
    

	private AnimatedSprite2D _animatedSprite;
	private Vector2 _direction;
	private Vector2 _velocity;
	private bool _isOnFloor; 

    public override void _Ready() {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _Process(double delta) {
		HandleSpriteDirection();
		HandleAnimation();
		
    }
	public override void _PhysicsProcess(double delta)
	{
		_velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor()) {
			_velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor()) {
			_velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// -1, 0, 1
		_direction = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
		if (_direction != Vector2.Zero) {
			_velocity.X = _direction.X * (Speed);
		}
		else {
			_velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = _velocity;
		MoveAndSlide();
	}

	private void HandleSpriteDirection() {

		if(_direction.X > 0) {
			_animatedSprite.FlipH = false;
		}
		else if (_direction.X < 0) {
			_animatedSprite.FlipH = true;
		}
	}

	private void HandleAnimation() {
		_isOnFloor = IsOnFloor();
        
		if (!_isOnFloor && _animatedSprite.Animation != "fall") {
			_animatedSprite.Play("fall");
		}
		else if (_velocity.X != 0 && _isOnFloor && !Input.IsKeyPressed(Key.Shift)) {
			_animatedSprite.Play("walk");
			Speed = BaseSpeed;
		}
		else if (_velocity.X != 0 && _isOnFloor && Input.IsKeyPressed(Key.Shift)) {
			_animatedSprite.Play("run");
			Speed = Sprint;
		}
		else if (_velocity.X == 0 && _isOnFloor) {
			_animatedSprite.Play("idle");
			Speed = BaseSpeed;
		}
	}
}
