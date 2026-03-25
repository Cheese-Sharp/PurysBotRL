using RedUtils;
using RedUtils.Math;
using RLBot.Manager;
using System;
using System.Drawing;
/* 
 * This is the main file. It contains your bot class. Feel free to change the name!
 * An instance of this class will be created for each instance of your bot in the game.
 * Your bot derives from the "RedUtilsBot" class, contained in the Bot file inside the RedUtils project.
 * The run function listed below runs every tick, and should contain the custom strategy code (made by you!)
 * Right now though, it has a default ball chase strategy. Feel free to read up and use anything you like for your own strategy.
*/
namespace Bot
{
	// Your bot class! :D
	public class RedBot : RUBot
	{
		// Runs every tick. Should be used to find an Action to execute
		public Vec3 shotStart = new Vec3(Vec3.Zero), shotTarget = new Vec3(Vec3.Zero);
		public bool testBehavior = true;
		private float ballTrailTimer;
		private float ballTrailTime = 0.05f;
		Vec3[] ballPoints = new Vec3[720];
		public enum BotStates
		{
			Defense, Neutral, Offense
		}
		public BotStates state;
		public override void Run()
		{
			// Prints out the current action to the screen, so we know what our bot is doing
			Renderer.Text2D(Action != null ? Action.ToString() : "", new Vec3(0f, 10f), 1f, Color.Red);

			Vec3 ToBall = Ball.Location - Me.Location;
			float toBallDot = Me.Forward.Dot(ToBall);

			Vec3 ToNet = TheirGoal.Location - Me.Location;
			float toNetDot = Me.Forward.Dot(ToNet);

			Renderer.Text2D($"ToBall: {toBallDot}, ToNet: {toNetDot}, behindBall {Me.Location.Dist(TheirGoal.Location) - Ball.Location.Dist(TheirGoal.Location)}", new Vec3(0, 170), 2, Color.Violet);

			if (ballTrailTimer > ballTrailTime * 120)
			{
				ballPoints = Utils.BallPathPoints(this);
				ballTrailTimer = 0;
			}
			else
			{
				ballTrailTimer++;
			}
			Renderer.Polyline3D(ballPoints, Color.Azure);


			if (IsKickoff && Action == null)
			{
				bool goingForKickoff = true; // by default, go for kickoff
				foreach (Car teammate in Teammates)
				{
					// if any teammates are closer to the ball, then don't go for kickoff
					goingForKickoff = goingForKickoff && Me.Location.Dist(Ball.Location) <= teammate.Location.Dist(Ball.Location);
				}

				Action = goingForKickoff ? new Kickoff() : new GetBoost(Me, interruptible: false); // if we aren't going for the kickoff, get boost
				Action = new Dribble(Me);
			}
			else if (Action == null || ((Action is Drive) && Action.Interruptible))
			{
				float ballToNetDist = Ball.Location.Dist(OurGoal.Location);
				if (ballToNetDist > 98000)
				{
					state = BotStates.Offense;
				}
				else if (ballToNetDist > 96000)
				{
					state = BotStates.Neutral;
				}
				else
				{
					state = BotStates.Defense;
				}

				if (testBehavior)
				{
					TestRun();
					return;
				}

				switch (state)
				{
					case BotStates.Defense:
						DefenseRun();
						break;

					case BotStates.Neutral:
						NeutralRun();
						break;

					case BotStates.Offense:
						AttackRun();
						break;
				}
				return;
			}
		}
		private void DefenseRun()
		{
			Shot shot = FindShot(DefaultShotCheck, new Target(TheirGoal));
			if (shot != null && shot.Slice.Time - Game.Time < 1.5f)
			{
				Action = shot;
			}
			else
			{
				Action = new ShadowDefense(Me);
			}	
		}
		private void AttackRun()
		{
			Shot shot = FindShot(DefaultShotCheck, new Target(TheirGoal));
			Action = shot ?? Action ?? new Drive(Me, Ball.Location);
		}
		private void NeutralRun()
		{
			Action = new Drive(Me, new Vec3(Ball.Location.x, 0, Ball.Location.z));
		}
		private void TestRun()
		{
			Action = Action is not Dribble ? new Dribble(Me) : Action;
			//Vec3 ToBall = Ball.Location - Me.Location;
			//float toBallDot = Me.Forward.Dot(ToBall);

			//Vec3 ToNet = TheirGoal.Location - Me.Location;
			//float toNetDot = Me.Forward.Dot(ToNet);

			//Renderer.Text2D($"ToBall: {toBallDot}, ToNet: {toNetDot}", new Vec3(0, 170), 2, Color.Violet);

			//// if behind ball and facing towards opp net
			//bool behindBall = Me.Location.Dist(TheirGoal.Location) > Ball.Location.Dist(TheirGoal.Location);
			//behindBall = Me.Location.Dist(TheirGoal.Location) - Ball.Location.Dist(TheirGoal.Location) > - 1000;
			//if (behindBall && toNetDot > -0.5)
			//	Action = Action is not Dribble ? new Dribble(Me) : Action;
			//else
			//// else shot the ball
			//{
			//	Shot shot = FindShot(DefaultShotCheck, new Target(TheirGoal));
			//	Action = shot ?? Action ?? new GetBoost(Me, interruptible: true);
			//}



			//Shot testShot = FindShot(TestShotCheck, new Target(TheirGoal));
			//Action = testShot ?? Action ?? new Drive(Me, OurGoal.Location);

		}
	}
}
