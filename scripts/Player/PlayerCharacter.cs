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

    enum PlayerState {
        Idle,
        Walk,
        Run,
        Jump,
        LongJump,
        Fall,
        DeadHang,
        CatHang,
        Death
    }

    private AnimatedSprite2D _animatedSprite;
	private Vector2 _direction;
	private Vector2 _velocity;
	
	private bool _isOnFloor;

	private PlayerState _state = PlayerState.Idle;
	private String _currentAnim = "";

    private float _fallStartY = 0;
    private float _fallDistanceThreshold = 200f;
    private bool _wasOnFloor = false;
	private bool _isDead = false;



    public override void _Ready() {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _Process(double delta) {
		HandleSpriteDirection();
		UpdateState();
		HandleAnimation();
		
    }
	public override void _PhysicsProcess(double delta)
	{
		
		_velocity = Velocity;
		_isOnFloor = IsOnFloor();
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

		// Death
		HandleDeath();

    }

	private void HandleSpriteDirection() {

		if(_direction.X > 0) {
			_animatedSprite.FlipH = false;
		}
		else if (_direction.X < 0) {
			_animatedSprite.FlipH = true;
		}
	}

	private void UpdateState() {
        if (Input.IsKeyPressed(Key.Shift) && Input.IsActionJustPressed("jump") && _velocity.X != 0) {
			_state = PlayerState.LongJump;
        }
        else if (!_isOnFloor && (_animatedSprite.Animation != "fall" && _animatedSprite.Animation != "longJump")) {
			_fallStartY = GlobalPosition.Y;
            _state = PlayerState.Fall;
        }
        else if (_velocity.X != 0 && _isOnFloor && !Input.IsKeyPressed(Key.Shift)) {
            _state = PlayerState.Walk;
        }
        else if (_velocity.X != 0 && _isOnFloor && Input.IsKeyPressed(Key.Shift)) {
            _state = PlayerState.Run;
        }
        else if (_velocity.X == 0 && _isOnFloor) {
            _state = PlayerState.Idle;
        }

		// for death and other things related
        if (_isDead) {
			_state = PlayerState.Death;
        }
    }

	private void HandleAnimation() {

		switch (_state) {
			case PlayerState.Idle:
                PlayAnim("idle");
				Speed = BaseSpeed;
				break;

			case PlayerState.Walk:
                PlayAnim("walk");
				Speed = BaseSpeed;
				break;

			case PlayerState.Run:
                PlayAnim("run");
				Speed = Sprint;
				break;

			case PlayerState.Jump:
                PlayAnim("jump");
				break;

			case PlayerState.LongJump:
                PlayAnim("longJump");
				break;

			case PlayerState.Fall:
                PlayAnim("fall");
				break;

			case PlayerState.DeadHang:
                PlayAnim("deadHang");
				break;

			case PlayerState.CatHang:
                PlayAnim("catHang");
				break;

			case PlayerState.Death:
                PlayAnim("death");
                Velocity = Vector2.Zero;
                Speed = 0;
				Die();
                break;

		}
		
	}
	private void Die() {
        GetTree().CreateTimer(1.0f).Timeout += () => RestartLevel();
    }
    private void PlayAnim(string animName) {
        if (_currentAnim != animName) {
            _animatedSprite.Play(animName);
            _currentAnim = animName;
        }
    }
	 private void HandleDeath() {
        if (!_wasOnFloor && _isOnFloor) {
            float fallDistance = GlobalPosition.Y - _fallStartY;

            if (fallDistance > _fallDistanceThreshold) {
                _isDead = true;

            }

        }
        if (_wasOnFloor && !_isOnFloor) {
            _fallStartY = GlobalPosition.Y;
        }
        _wasOnFloor = _isOnFloor;
    }
    private void RestartLevel() {
        var currentScene = GetTree().CurrentScene;
        GetTree().ReloadCurrentScene(); // Shortcut in Godot 4.2+
    }
}
