using RedUtils.Math;
using RedUtils;
using System;
using RLBot.Manager;

namespace RedUtils
{
	/// <summary>An action to be performed by our bot</summary>
	public class ShadowDefense : IAction
	{
		/// <summary>Whether or not the action has finished</summary>
		public bool Finished { get; private set; }
		/// <summary>Whether or not the action can be interrupted</summary>
		public bool Interruptible { get; private set; }
		/// <summary>This action's arrive sub action, which will take us to the ball</summary>
		public Arrive ArriveAction;

		public ShadowDefense(Car car)
		{
			Interruptible = true;
			Finished = false;
			ArriveAction = new Arrive(car, new Vec3(0, 0, 0), Ball.Location.Direction(Field.BlueGoal.Location), -1, true, 0);
		}
		/// <summary>Performs this action</summary>
		public void Run(RUBot bot)
		{
			Shot shot = RUBot.FindShot(bot.DefaultShotCheck, new Target(bot.OurGoal, true));
			Shot bangShot = RUBot.FindShot(bot.DefaultShotCheck, new Target(bot.TheirGoal));
			Shot shotToTake;
			if (shot != null && bangShot != null)
			{
				// Get fastest shot
				shotToTake = shot;
			}
			else if (shot != null && bangShot == null)
			{
				shotToTake = shot;
			}
			else if (shot == null && bangShot != null)
			{
				shotToTake = bangShot;
			}
			else
			{
				shotToTake = null;
			}
			Interruptible = ArriveAction.Interruptible;

			if (shotToTake != null && shotToTake.Slice.Time - Game.Time < 1.5f)
			{
				bot.Action = shotToTake;
				return;
			}
			Vec3 shadowPosition = Ball.Location + (Ball.Location.FlatDirection(bot.OurGoal.Location) * (Ball.Location.FlatDist(bot.OurGoal.Location) / 2));
			bot.Renderer.Line3D(shadowPosition, shadowPosition + Field.NearestSurface(shadowPosition).Normal * 200, System.Drawing.Color.DarkViolet);
			ArriveAction.Target = shadowPosition;
			ArriveAction.Direction = Ball.Location.Direction(bot.OurGoal.Location);
			ArriveAction.Run(bot);
		}
	}
}
