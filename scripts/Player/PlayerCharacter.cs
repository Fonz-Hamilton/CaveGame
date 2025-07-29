using Godot;
using System;
using static Godot.WebSocketPeer;

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

    // nodes
    private AnimatedSprite2D _animatedSprite;
	private RayCast2D _ledgeDetectionTop;			// for dead hang
    private RayCast2D _ledgeDetectionMiddle;		// for cat hang
    private RayCast2D _ledgeDetectionClearance;
    private CollisionShape2D _collisionShape;

    // movement
	private Vector2 _direction;
	private Vector2 _velocity;
	private bool _isOnFloor;
	private Vector2 _baseGravity = new Vector2(0, 980);

    // state and animation
	private PlayerState _state = PlayerState.Idle;
	private String _currentAnim = "";
    private Vector2 _collisionShapePos;

    // Death stuff
    private float _fallStartY = 0;
    private float _fallDistanceThreshold = 200f;
    private bool _wasOnFloor = false;				// tracks if previous frame was on floor
	private bool _isDead = false;

    // ledge stuff
	private Vector2 _ledgeDetectionTopPos;			// for dead hang inspector position
    private Vector2 _ledgeDetectionMiddlePos;		// for cat hang inspector position
	private Vector2 _ledgeDetectionClearancePos;
    private Vector2 _ledgeGrabPosition = Vector2.Zero;
    private float _ledgeGrabExitDistance = 10f;     // have moved far enough away to regrab
    private float _pushDownVelocity = 10f;
    private bool _isOnLedge = false;
    private bool _canGrabLedge = false;
    private bool _transitionToCatHang = false;
    private bool _wantsToClimb = false;
    private bool _wantsToDrop = false;

    // misc helpers
	private Vector2 _vectorFlip =  new Vector2(-1, 1);


    public override void _Ready() {
		// Animation
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		Gravity = _baseGravity;
        
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
			_velocity += Gravity * (float)delta;
			
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

        // ledge stuff
        if (Input.IsActionJustPressed("move_down") && !_isOnFloor) {
            _wantsToDrop = true;
            GD.Print("wants to drop: " + _wantsToDrop);
        }

        if ((Input.IsActionJustPressed("move_up") || Input.IsActionJustPressed("jump")) && !_isOnFloor) {
            _wantsToClimb = true;
            GD.Print("wants to climb: " + _wantsToClimb);
        }

        //if (SomeConditionToTransitionToCatHang()) {
        //    _transitionToCatHang = true;
        //}

        // Motion
        Velocity = _velocity;
		MoveAndSlide();


		// Death
		HandleFallDeath();
        
    }

	

	private void UpdateState() {
        GD.Print("State Top: " + _state.ToString());
        float distanceFromLastLedge = GlobalPosition.DistanceTo(_ledgeGrabPosition);

        if (!_canGrabLedge && distanceFromLastLedge > _ledgeGrabExitDistance) {
            _canGrabLedge = true;
            GD.Print("can crab ledge: " + _canGrabLedge);
        }
        if (Input.IsKeyPressed(Key.Shift) && Input.IsActionJustPressed("jump") && _velocity.X != 0 && _state != PlayerState.Fall) {
			_state = PlayerState.LongJump;
		}
        else if (!_isOnFloor && _canGrabLedge && !_ledgeDetectionClearance.IsColliding() && _ledgeDetectionTop.IsColliding()) {
            _state = PlayerState.DeadHang;
			HandleLedgeGrab();
            
            
            
        }
        else if ((!_isOnFloor && _canGrabLedge && !_ledgeDetectionClearance.IsColliding() && !_ledgeDetectionTop.IsColliding() &&_ledgeDetectionMiddle.IsColliding()) || _transitionToCatHang) {
            _state = PlayerState.CatHang;
            HandleLedgeGrab();
            


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


        // for death and other things related
        if (_isDead) {
			_state = PlayerState.Death;
            
            Die();
        }

        // states that need a refreshin'
        _wantsToDrop = false;
        _wantsToClimb = false;
        _transitionToCatHang = false;

        // Debug State
        GD.Print("State Bottom: " + _state.ToString());
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
        Velocity = Vector2.Zero;
        Speed = 0;
        JumpVelocity = 0; 
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

	private void HandleLedgeGrab() {
        _isOnLedge = true;
        _velocity = Vector2.Zero;
        Speed = 0;
        Gravity = Vector2.Zero;

        if (Input.IsActionJustPressed("jump") || Input.IsActionJustPressed("move_up")) {
            ClimbUp();					
        }
        else if (Input.IsActionJustPressed("move_down")) {
            DropFromLedge();
        }
        _ledgeGrabPosition = GlobalPosition;
    }

    
	private void DropFromLedge() {
        Gravity = _baseGravity;
        
        _isOnLedge = false;
        _canGrabLedge = false;
        _velocity.Y = _pushDownVelocity; // Push the player downward a bit
    }

    
    private void ClimbUp() {
        if(_state == PlayerState.DeadHang) {
            _transitionToCatHang = true;
            // Fix magic number
            // will wait until tilemap is ready
            _velocity.Y = 21 * (_ledgeDetectionTopPos.Y - _ledgeDetectionMiddlePos.Y);
        }

	}

    private void HandleSpriteDirection() {

        if (_direction.X > 0 && !_isOnLedge && !_isDead) {
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
        else if (_direction.X < 0 && !_isOnLedge && !_isDead) {
            _animatedSprite.FlipH = true;

            // raycast flip left
            _ledgeDetectionTop.Position = _ledgeDetectionTopPos * _vectorFlip;
            _ledgeDetectionTop.RotationDegrees = 180f;

            _ledgeDetectionMiddle.Position = _ledgeDetectionMiddlePos * _vectorFlip;
            _ledgeDetectionMiddle.RotationDegrees = 180f;

            _ledgeDetectionClearance.Position = _ledgeDetectionClearancePos * _vectorFlip;
            _ledgeDetectionClearance.RotationDegrees = 180f;

            // collision box flip right
            _collisionShape.Position = _collisionShapePos * _vectorFlip;
        }
    }
    private void RestartLevel() {
        var currentScene = GetTree().CurrentScene;
        GetTree().ReloadCurrentScene(); 
    }
}
