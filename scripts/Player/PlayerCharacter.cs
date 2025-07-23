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
	[Export]
	public Vector2 Gravity = new Vector2(0, 980);

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
	private RayCast2D _ledgeDetectionTop;			// for dead hang
    private RayCast2D _ledgeDetectionMiddle;		// for cat hang
    private RayCast2D _ledgeDetectionClearance;
    private CollisionShape2D _collisionShape;
	private Vector2 _direction;
	private Vector2 _velocity;
	
	private bool _isOnFloor;

	private PlayerState _state = PlayerState.Idle;
	private String _currentAnim = "";

    private float _fallStartY = 0;
    private float _fallDistanceThreshold = 200f;
    private bool _wasOnFloor = false;				// tracks if previous frame was on floor
	private bool _isDead = false;
	private Vector2 _ledgeDetectionTopPos;				// for dead hang inspector position
    private Vector2 _ledgeDetectionMiddlePos;		// for cat hang inspector position
	private Vector2 _ledgeDetectionClearancePos;
    private Vector2 _collisionShapePos;
	private Vector2 _vectorFlip =  new Vector2(-1, 1);


    public override void _Ready() {
		// Animation
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		Gravity = new Vector2(0, 980);

        // Basic raycast nodes
        _ledgeDetectionTop = GetNode<RayCast2D>("RayLedgeCheckTop");
		_ledgeDetectionMiddle = GetNode<RayCast2D>("RayLedgeCheckMiddle");
        _ledgeDetectionClearance = GetNode<RayCast2D>("RayLedgeCheckClearance");

        // Collisions
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

		// at start, get the position of the nodes to make editing easier
		_ledgeDetectionTopPos = _ledgeDetectionTop.Position;
		_ledgeDetectionMiddlePos = _ledgeDetectionMiddle.Position;
		_ledgeDetectionClearancePos = _ledgeDetectionClearance.Position;

		_collisionShapePos = _collisionShape.Position;
    }

    public override void _Process(double delta) {
		HandleSpriteDirection();
        
        HandleAnimation();
		
    }
	public override void _PhysicsProcess(double delta)
	{
        
        _velocity = Velocity;
		_isOnFloor = IsOnFloor();
        UpdateState();

        // Add the gravity.
        if (!IsOnFloor()) {
			_velocity += GetGravity() * (float)delta;
			//GD.Print("gravity: " + GetGravity().ToString());
			GD.Print("gravity: " + Gravity.ToString());
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

		// Raycast bullshit
		if(_ledgeDetectionTop.IsColliding()) {
			
			
			
			
		}

		// Death
		HandleFallDeath();
        
    }

	private void HandleSpriteDirection() {

		if(_direction.X > 0) {
			_animatedSprite.FlipH = false;
			
			// raycast flip right
			_ledgeDetectionTop.Position = _ledgeDetectionTopPos;
            _ledgeDetectionTop.RotationDegrees = 0f;

			_ledgeDetectionMiddle.Position = _ledgeDetectionMiddlePos;
            _ledgeDetectionMiddle.RotationDegrees = 0f;

			_ledgeDetectionClearance.Position = _ledgeDetectionClearancePos;
			_ledgeDetectionClearance.RotationDegrees = 0f;

            // collision box flip right
            _collisionShape.Position = _collisionShapePos;
        }
		else if (_direction.X < 0) {
			_animatedSprite.FlipH = true;
			
			// raycast flip left
			_ledgeDetectionTop.Position = _ledgeDetectionTopPos * _vectorFlip;
			_ledgeDetectionTop.RotationDegrees = 180f;

            _ledgeDetectionMiddle.Position = _ledgeDetectionMiddlePos * _vectorFlip;
            _ledgeDetectionMiddle.RotationDegrees = 180f;

            _ledgeDetectionClearance.Position = _ledgeDetectionClearancePos * _vectorFlip;
            _ledgeDetectionClearance.RotationDegrees = 180f;

            // collision box flipt right
            _collisionShape.Position = _collisionShapePos * (new Vector2(-1,1));
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
            Speed = BaseSpeed;
        }
		else if (_velocity.X != 0 && _isOnFloor && Input.IsKeyPressed(Key.Shift)) {
			_state = PlayerState.Run;
            Speed = Sprint;
        }
		else if (_velocity.X == 0 && _isOnFloor) {
			_state = PlayerState.Idle;
            Speed = BaseSpeed;
        }
		
		else if (!_isOnFloor && !_ledgeDetectionClearance.IsColliding() && _ledgeDetectionTop.IsColliding()) {
			_state = PlayerState.DeadHang;
            _velocity = Vector2.Zero;
			Speed = 0;
			Gravity = Vector2.Zero;
        }
		

		// for death and other things related
		if (_isDead) {
			_state = PlayerState.Death;
            Velocity = Vector2.Zero;
            Speed = 0;
            Die();
        }
    }

	private void HandleAnimation() {

		switch (_state) {
			case PlayerState.Idle:
                PlayAnim("idle");
				break;

			case PlayerState.Walk:
                PlayAnim("walk");
				break;

			case PlayerState.Run:
                PlayAnim("run");
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
	 private void HandleFallDeath() {
        if (!_wasOnFloor && _isOnFloor) {
            float fallDistance = GlobalPosition.Y - _fallStartY;

            if (fallDistance > _fallDistanceThreshold) {
                _isDead = true;

            }

        }
        if (_wasOnFloor && !_isOnFloor) {
            _fallStartY = GlobalPosition.Y;
        }
		// update 'previous frame' floor state for use in next frame
        _wasOnFloor = _isOnFloor;
    }
    private void RestartLevel() {
        var currentScene = GetTree().CurrentScene;
        GetTree().ReloadCurrentScene(); // Shortcut in Godot 4.2+
    }
}
