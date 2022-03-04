using Godot;
using System;

public class WeaponPivot : Spatial
{
    Spatial player;
    Spatial vertical_pivot;

    static Vector2 pos_0 = new Vector2(-15f, -16.85f);
    static Vector2 pos_1 = new Vector2(0f, -18f);
    static Vector2 pos_2 = new Vector2(15f, -16.85f);
    Vector2 current_pos = pos_0;

    float mouse_sense;
    float max_move_mult = 0.2f;
    float min_move_mult = 0.05f;
    float start_pivot_v = pos_0.y;
    float start_pivot_h = pos_0.x;
    float base_focus_timeout = 2.5f;
    float focus_timeout;
    float hdiff = 0;
    float vdiff = 0;

    int position = 0;

    bool is_sprinting;
    bool is_sliding;

    public override void _Ready()
    {
        player = GetNode<Spatial>("../../../../");
        mouse_sense = max_move_mult * (float)player.Get("mouse_sense");
        vertical_pivot = GetNode<Spatial>("VerticalPivot");
        RotateY(Mathf.Deg2Rad(start_pivot_h));
        vertical_pivot.RotateX(Mathf.Deg2Rad(start_pivot_v));

        is_sprinting = (bool)player.Get("is_sprinting");
        is_sprinting = (bool)player.Get("is_sliding");

    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.GetMouseMode() == Input.MouseMode.Captured) {
            if (Input.GetActionStrength("focus") > 0)
            {
                RotateY(Mathf.Deg2Rad(-mouseMotion.Relative.x * mouse_sense / (hdiff + 1)));
                vertical_pivot.RotateX(Mathf.Deg2Rad(-mouseMotion.Relative.y * mouse_sense / (vdiff + 1)));
                Vector3 rotDeg = vertical_pivot.RotationDegrees;
                rotDeg.x = Mathf.Clamp(rotDeg.x, current_pos.x - 5, current_pos.x + 5); 
                vertical_pivot.RotationDegrees = rotDeg;
                Vector3 hRotDeg = RotationDegrees;
                hRotDeg.y = Mathf.Clamp(hRotDeg.y, current_pos.y - 5, current_pos.y + 5);
                RotationDegrees = hRotDeg;
            }
        }
    }
    public override void _Process(float delta)
    {
        hdiff = Mathf.Abs(RotationDegrees.y - current_pos.y);
        vdiff = Mathf.Abs(vertical_pivot.RotationDegrees.x - current_pos.x);

        if (is_sprinting)
            //rest function
        if (Input.IsActionJustReleased("focus") && position > 2)
            focus_timeout = base_focus_timeout;
        if (focus_timeout > 0 && Input.GetActionStrength("focus") > 0)
            focus_timeout -= delta;
        //else if ( dist to nearest point not 0)
            //move to nearest pt
    }
}
