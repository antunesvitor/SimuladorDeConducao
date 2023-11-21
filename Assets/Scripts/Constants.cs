using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const float FEEDBACK_COLLISION = -.05f;
    public const float FEEDBACK_COLLISION_SIDEWALK = -.05f;
    public const float FEEDBACK_COLLISION_WALL = -.05f;
    public const float FEEDBACK_MAXSTEPS_REACHED = -0f;                    //Esta é aplicada quando ele chega ao final do EP e não cumpriu o objetivo
    public const float FEEDBACK_MAXSTEPS_REACHED_PER_STEP = -.7f;             //Esta é aplicada por step, pouco a pouco dentro do episódio
    public const float FEEDBACK_CAR_UPSIDE_DOWN = -1f;
    public const float FEEDBACK_MAX_CHECKPOINT_REACHED = .8f;
    public const float FEEDBACK_DESTINATION_REACHED = .2f;

}
