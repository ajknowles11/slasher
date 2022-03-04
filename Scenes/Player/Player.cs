using Godot;
using System.Collections.Generic;

public class Player : KinematicBody
{
    Dictionary<string, int> accel_type = new Dictionary<string, int>();

    Spatial head_trans;
    Spatial head;
    CollisionShape p_cap;
    CollisionShape foot;
    RayCast bonk;
    AnimationPlayer cam_anim;
    Camera camera;
    Camera weapon_cam;

    float base_speed = 14f;
    float speed = 14f;
    float sprint_speed_mult = 1.4f;
    float sprint_accel_mult = 5f;
    float slide_speed_mult = 2f;
    float slide_input_time = 2.5f;
    float slide_input_mult = 0.8f;
    float crouch_speed_mult = 0.8f;
    float crouch_speed = 10f;
    float base_slide_time = 0.55f;
    float slide_time;
    float gravity = 48f;
    float jump = 16f;
    float cam_accel = 40f;
    float fov;
    float fov_sprint_mult = 1.05f;
    float fov_change_mult = 12f;
    float mouse_sense = 0.1f;
    float default_height;
    float crouch_height_mult = 0.4f;
    float slide_crouch_mult = 1.5f;
    float slide_deccel_mult = 0.35f;
    float slide_rot = 5f;

    Vector3 snap;
    Vector3 direction = new Vector3();
    Vector3 velocity = new Vector3();
    Vector3 gravity_vec = new Vector3();
    Vector3 movement = new Vector3();
    Vector3 head_pos = new Vector3();
    Vector3 foot_pos = new Vector3();
    Vector3 slide_direction = new Vector3();

    Vector2 aspect_ratio = new Vector2();

    int accel;

    bool is_paused = false;
    bool is_sprint = false;
    bool is_sprinting = false;
    bool is_crouch = false;
    bool toggle_sprint = false;
    bool is_slide = false;
    bool is_sliding = false;
    bool is_slide_rotate = false;

    public override void _Ready()
    {
        accel_type.Add("default", 7);
        accel_type.Add("air", 1);
        accel = accel_type["default"];
        head_trans = GetNode<Spatial>("HeadVerticalTranslate");
        head = head_trans.GetNode<Spatial>("Head");
        camera = head.GetChild<Camera>(0);
        cam_anim = camera.GetNode<AnimationPlayer>("AnimationPlayer");
        weapon_cam = camera.GetNode<ViewportContainer>("ViewportContainer").GetNode<Viewport>("Viewport").GetNode<Camera>("WeaponCamera");
        head_pos = head.GlobalTransform.origin;

        p_cap = GetNode<CollisionShape>("CollisionShape");
        default_height = ((CapsuleShape)p_cap.Shape).Height;
        foot = GetNode<CollisionShape>("Foot");
        foot_pos = foot.GlobalTransform.origin;
        bonk = p_cap.GetNode<RayCast>("HeadBonk");
        Input.SetMouseMode(Input.MouseMode.Captured);

        aspect_ratio.x = 1920f;
        aspect_ratio.y = 1080f;

        fov = GetVertical(100f);
        camera.Fov = fov;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && !is_paused) {
            if (Input.GetActionStrength("focus") == 0)
            {
                RotateY(Mathf.Deg2Rad(-mouseMotion.Relative.x * mouse_sense));
                head.RotateX(Mathf.Deg2Rad(-mouseMotion.Relative.y * mouse_sense));
                Vector3 rotDeg = head.RotationDegrees;
                rotDeg.x = Mathf.Clamp(rotDeg.x, -89f, 89f);
                head.RotationDegrees = rotDeg;
            }
            //else make it move slightly at edges
        }

        if (Input.IsActionJustPressed("stop"))
            if (!is_paused)
            {
                is_paused = true;
                Input.SetMouseMode(Input.MouseMode.Visible);
            }
            else
            {
                is_paused = false;
                Input.SetMouseMode(Input.MouseMode.Captured);
            }

        if (!toggle_sprint)
        {
            if (Input.GetActionStrength("sprint") != 0)
                is_sprint = true;
            else 
                is_sprint = false;
        }

        if (toggle_sprint && Input.IsActionJustPressed("sprint"))
            is_sprint = !is_sprint;
    }

    public override void _Process(float delta)
    {
        if (Engine.GetFramesPerSecond() > Engine.IterationsPerSecond) 
        {
            camera.SetAsToplevel(true);

            Vector3 Gtrans = head.GlobalTransform.origin;
	    	
	    
	    var cameraGT = camera.GlobalTransform;
        cameraGT.origin = camera.GlobalTransform.origin.LinearInterpolate(Gtrans, cam_accel * delta);
	    camera.GlobalTransform = cameraGT;

            Vector3 camRot = camera.Rotation;
            camRot.y = Rotation.y;
            camRot.x = head.Rotation.x;
            camera.Rotation = camRot;
        } else 
        {
            camera.SetAsToplevel(false);
            camera.GlobalTransform = head.GlobalTransform;
        }

        weapon_cam.GlobalTransform = camera.GlobalTransform;
    }

    public override void _PhysicsProcess(float delta)
    {
        var is_bonk = false;
        if (bonk.IsColliding())
            is_bonk = true;

        direction = Vector3.Zero;
        var h_rot = GlobalTransform.basis.GetEuler().y;
        var f_input = Input.GetActionStrength("move_backward") - Input.GetActionStrength("move_forward");
	    var h_input = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
	    direction = new Vector3(h_input, 0, f_input).Rotated(Vector3.Up, h_rot).Normalized();

        if (IsOnFloor()) 
        {
            snap = -GetFloorNormal();
		    accel = accel_type["default"];
		    gravity_vec = Vector3.Zero;
        } else 
        {
            snap = Vector3.Down;
		    accel = accel_type["air"];
		    gravity_vec += Vector3.Down * gravity * delta;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor()) 
        {
            snap = Vector3.Zero;
		    gravity_vec = Vector3.Up * jump;
        }

        if (is_bonk)
            gravity_vec = Vector3.Down * 2;

        var p_cap_shape = (CapsuleShape)p_cap.Shape; //dont forget to add mantle capabilities (check for mantle node or something?)
        
        if (is_crouch)
        {
            is_sprinting = false;
            if (is_slide)
            {
                if (!is_sliding)
                {
                    is_sliding = true;
                    slide_time = base_slide_time;
                    slide_direction = velocity.Normalized();
                    is_slide_rotate = true;
                }
                else if (Input.IsActionJustPressed("crouch"))
                {
                    is_slide = false;
                    is_sliding = false;
                    cam_anim.Stop(true);
                    cam_anim.Play("SlideEndTilt");
                    slide_time = base_slide_time;
                }
                else if (Input.IsActionJustPressed("jump"))
                {
                    is_slide = false;
                    is_sliding = false;
                    is_crouch = false;
                    cam_anim.Stop(true);
                    cam_anim.Play("SlideEndTilt");
                    slide_time = base_slide_time;
                }
                else if (IsOnFloor())
                {
                    if (slide_time > 0f)
                        slide_time -= delta;
                    else
                    {
                        is_slide = false;
                        is_sliding = false;
                        cam_anim.Stop(true);
                        cam_anim.Play("SlideEndTilt");
                    }

                    slide_direction = velocity.Normalized();
                    if (((CapsuleShape)p_cap.Shape).Height > default_height * crouch_height_mult)
                        ((CapsuleShape)p_cap.Shape).Height -= slide_crouch_mult * crouch_speed * delta;
                    if (p_cap.Translation.y > -(default_height * crouch_height_mult))
                        p_cap.Translate(slide_crouch_mult * crouch_speed * Vector3.Back * delta * 0.5f);
                    if (head_trans.GlobalTransform.origin.y > (foot.GlobalTransform.origin.y - foot_pos.y) - 1 + crouch_height_mult * (1 + head_pos.y))
                        head_trans.GlobalTranslate(slide_crouch_mult * crouch_speed * Vector3.Down * delta);
                    if (camera.Fov > fov)
                        camera.Fov -= slide_deccel_mult * fov_change_mult * accel * delta;
                    if (is_slide_rotate)
                    {
                        cam_anim.Play("SlideStartTilt");
                        is_slide_rotate = false;
                    }
                    if (speed < base_speed * slide_speed_mult)
                        speed += accel * sprint_accel_mult * delta;

                    direction = (slide_input_mult * direction + slide_direction).Normalized();
                    var slide_movement = (Mathf.Clamp(slide_input_time * (1 - (slide_time / base_slide_time)),0,1) * slide_input_mult * direction + slide_direction).Normalized();

                    velocity = velocity.LinearInterpolate(slide_movement * speed, accel * delta);
                }
            }
            else
            {
                if (Input.IsActionJustPressed("crouch"))
                    is_crouch = false;
                if (IsOnFloor())
                {
                    if (((CapsuleShape)p_cap.Shape).Height > default_height * crouch_height_mult)
                        ((CapsuleShape)p_cap.Shape).Height -= crouch_speed * delta;
                    if (p_cap.Translation.y > -(default_height * crouch_height_mult))
                        p_cap.Translate(crouch_speed * Vector3.Back * delta * 0.5f);
                    if (head_trans.GlobalTransform.origin.y > (foot.GlobalTransform.origin.y - foot_pos.y) - 1 + crouch_height_mult * (1 + head_pos.y))
                        head_trans.GlobalTranslate(crouch_speed * Vector3.Down * delta); 
                    if (camera.Fov > fov)
                        camera.Fov -= fov_change_mult * accel * delta;
                }

                speed = base_speed;
                velocity = velocity.LinearInterpolate(direction * speed * crouch_speed_mult, accel * delta);
            }
        }
        else
        {
            is_slide = false;
            is_sliding = false;

            if (!is_bonk)
            {
                if (((CapsuleShape)p_cap.Shape).Height < default_height)
                    ((CapsuleShape)p_cap.Shape).Height += crouch_speed * delta;
                if (p_cap.Translation.y < 0)
                    p_cap.Translate(crouch_speed * Vector3.Forward * delta * 0.5f);
                if (head_trans.GlobalTransform.origin.y < (foot.GlobalTransform.origin.y - foot_pos.y) + head_pos.y) 
                    head_trans.GlobalTranslate(crouch_speed * Vector3.Up  * delta);
            }

            if (Input.IsActionJustPressed("crouch"))
                is_crouch = true;

            if (velocity.LengthSquared() > (base_speed) * (base_speed) && Input.IsActionJustPressed("crouch"))
                is_slide = true;

            if (is_sprint && f_input < 0 && !(is_bonk && IsOnFloor()))
            { 
                is_sprinting = true;
                if (speed < base_speed * sprint_speed_mult)
                    speed += accel * sprint_accel_mult * delta;
                if (camera.Fov < fov * fov_sprint_mult)
                    camera.Fov += fov_change_mult * accel * delta;
            }
            else 
            {
                is_sprinting = false;
                if (speed > base_speed)
                    speed -= accel * sprint_accel_mult * delta;
                if (camera.Fov > fov)
                    camera.Fov -= fov_change_mult * accel * delta;
            }

            speed = Mathf.Clamp(speed, base_speed, base_speed * sprint_speed_mult);
            velocity = velocity.LinearInterpolate(direction * speed, accel * delta);
        }

        ((CapsuleShape)p_cap.Shape).Height = Mathf.Clamp(((CapsuleShape)p_cap.Shape).Height, default_height * crouch_height_mult, default_height);
        camera.Fov = Mathf.Clamp(camera.Fov, fov, fov * fov_sprint_mult);

        movement = velocity + gravity_vec;

	    MoveAndSlideWithSnap(movement, snap, Vector3.Up);
    }

    private float GetVertical(float h_fov)
    {
        var width = aspect_ratio.x;
        var height = aspect_ratio.y;

        var h_fov_rad = h_fov * Mathf.Pi / 180;
        var v_fov_rad = 2*Mathf.Atan(Mathf.Tan(h_fov_rad/2) * height/width);

        return v_fov_rad * 180 / Mathf.Pi;
    }

}