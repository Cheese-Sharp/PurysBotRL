using Google.FlatBuffers;
using RedUtils.Math;
using RLBot.Flat;
using RLBot.GameState;
using RLBot.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;

namespace RedUtils
{
	/// <summary>An action to be performed by our bot</summary>
	public class Dribble : IAction
	{
		/// <summary>Whether or not the action has finished</summary>
		public bool Finished { get; internal set; }
		/// <summary>Whether or not the action can be interrupted</summary>
		public bool Interruptible { get; internal set; }
		/// <summary>This shot's arrive sub action, which will take us to the ball</summary>
		public Arrive ArriveAction { get; internal set; }
		public Drive DriveAction { get; internal set; }

		private Vec3 DribbleTarget, WhereBallShouldBe;
		// Dribble Target without offsets
		private Vec3 OriginalTarget;
		private float toTargetDot;
		private float toTargetDotRight;
		float waitDuration = 5;
		float waitTimer = 999;
		float updateTargetDur = 0.1f, updateTargetTimer = 999;
		bool arrivedAtTarget = false;
		bool isDribbling = false;
		bool isCatching = false;
		int successfulArrives = 0;
		float predBallTime = 0;
		float dribbleSpeedUp = 5f;
		/// <summary>Initializes a new Dribble Action/// </summary>
		public Dribble(Car car) 
		{
			Finished = false;
			Interruptible = false;

			ArriveAction = new Arrive(car, Vec3.Zero);
			DriveAction = new Drive(car, Vec3.Zero);
		}
		/// <summary>Performs this action</summary>
		public void Run(RUBot bot) 
		{
			// Cancel dribble if bla bla bla
			bool behindBall = bot.Me.Location.Dist(bot.TheirGoal.Location) - Ball.Location.Dist(bot.TheirGoal.Location) > -1000;
			Interruptible = false;

			if ((DribbleTarget - bot.Me.Location).Length() < 120f)
			{
				waitTimer++;
				if (waitTimer > waitDuration * 120)
				{
					successfulArrives++;
					NewDribbleTarget();
					waitTimer = 0;
				}
			}
			else
			{
				waitTimer = Utils.Cap(waitTimer -= 2, 0, 9999);
			}

			// Debug
			bool testDrive = false;
			bool circleTarget = false;

			if(testDrive)
			{
				if (circleTarget)
				{
					MoveTargetInCircle(0.5f, 800);
				}
				else
				{
					DribbleTarget = OriginalTarget;
				}
				SetBallPos(DribbleTarget.Flatten() + Vec3.Up * -100f, bot);
			}

			Vec3 toNetDir = bot.TheirGoal.Location - bot.Me.Location;
			Vec3 toBallDir = bot.Me.Location.FlatDirection(Ball.Location);
			if (isDribbling)
			{
				// hard to stop dribbling
				isDribbling = (100 < Ball.Location.z && Ball.Location.z < 750) && MathF.Abs(Ball.Velocity.z) < 1000 && toBallDir.Length() < 125f;
			}
			else
			{
				// hard to start dribbling
				bool curBallIsCentered = Ball.Location.FlatDist(bot.Me.Location) < 50;
				bool curBallDribbled = (120 < Ball.Location.z && Ball.Location.z < 150);
				bool futureBallDribbled = (120 < Ball.Prediction.Slices[20].Location.z && Ball.Prediction.Slices[20].Location.z < 150);
				isDribbling = curBallIsCentered && (curBallDribbled && futureBallDribbled && MathF.Abs(Ball.Velocity.z) < 125 && toBallDir.Length() < 100f);
			}


			Vec3 toTarget = DribbleTarget - bot.Me.Location;
			bot.Renderer.Line3D(DribbleTarget, DribbleTarget + Vec3.Up * (50 + waitTimer), System.Drawing.Color.LightCyan);

			bot.Renderer.Text2D($"drib: {-Utils.Cap(dribbleSpeedUp / (2f), 0, 70)}", new Vec3(0, 70), 2, isDribbling ? System.Drawing.Color.Green : System.Drawing.Color.Yellow);

			if (isDribbling)
			{
				dribbleSpeedUp++;
				Vec3 wideNet = bot.TheirGoal.Location;
				wideNet.x = Utils.Cap(Ball.Location.x, -750, 750);

				Car closestEnemy = Utils.GetClosestEnemy(bot, bot.Me.Location);
				bool enemyFacingBall = closestEnemy.Velocity.Normalize().Dot(closestEnemy.Location.Direction(Ball.Location)) > 0.6f;
				bool enemyChallenging = (closestEnemy.HasJumped || closestEnemy.Location.z > 120f) && MathF.Abs(Ball.Velocity.z) < 75 && closestEnemy.Location.FlatDist(Ball.Location) < 1550 && enemyFacingBall;
				if (enemyChallenging) //|| wideNet.Dist(bot.Me.Location) < 850f)
				{
					//SetBallPos(WhereBallShouldBe, bot);
					bot.Action = new Dodge((toNetDir.Flatten() + Vec3.Up * 200).Normalize());
					return;
				}
				
				CarryToLocation(wideNet, bot);
			}
			else
			{
				// Catch
				//CarryWithOffset(-Ball.Velocity.Normalize() * 5, bot);
				CarryWithOffset(toNetDir.FlatNorm() * 20, bot);
				dribbleSpeedUp -= 0.1f;
				if (dribbleSpeedUp < 0)
					dribbleSpeedUp = 0;
			}
				
		}
		private void NewDribbleTarget()
		{
			Console.WriteLine("Setting new target!");
			Random rnd = new Random();
			OriginalTarget = new Vec3(rnd.Next(-2000, 2000), rnd.Next(-5000, 5000), 0);
			//OriginalTarget = new Vec3(Utils.Cap(OriginalTarget.x, -3000, 3000), Utils.Cap(OriginalTarget.x, -5000, 5000), 100);
		}
		private void DribbleControl(RUBot bot)
		{
			Vec3 ballToTarget = DribbleTarget.Flatten() - Ball.Location.Flatten();
			Vec3 carToTargetVec = DribbleTarget - bot.Me.Location;
			DribbleTarget = Ball.Location.Flatten() - (bot.TheirGoal.Location - Ball.Location.Flatten()).Normalize() * 80 + Vec3.Up * 100;
			bot.Renderer.Line3D(DribbleTarget, DribbleTarget + Vec3.Up * 200, System.Drawing.Color.DarkOliveGreen);
			bot.Renderer.Line3D(Ball.Location, Ball.Location + Vec3.Up * 200, System.Drawing.Color.LimeGreen);
			bot.Renderer.Line3D(Ball.Location + ballToTarget, Ball.Location + (ballToTarget * ballToTarget.Length()), System.Drawing.Color.Yellow);

			toTargetDot = bot.Me.Forward.Dot(bot.Me.Location.Flatten().Direction(DribbleTarget));
			toTargetDotRight = bot.Me.Right.Dot(bot.Me.Location.Direction(DribbleTarget));
			bot.Renderer.Text3D($"Dribbling", bot.Me.Location + new Vec3(0, 0, 100), 2,	System.Drawing.Color.OrangeRed);

			if (toTargetDot < -0.1 && carToTargetVec.Length() > 800)
			{
				ArriveAction.Target = DribbleTarget;
				ArriveAction.Run(bot);
				return;
			}

			bot.Throttle(Utils.Cap(Ball.Velocity.Length() * 1.5f, 1, Car.MaxSpeed), MathF.Sign(toTargetDot) >= 0 ? false : true);
			bot.Controller.Steer = Utils.Cap(toTargetDotRight * 2, -1, 1);
		}
		private void ToBallDribbleControl(RUBot bot)
		{
			Vec3 ballToTarget = DribbleTarget.Flatten() - Ball.Location.Flatten();
			Vec3 carToTargetVec = DribbleTarget - bot.Me.Location;
			DribbleTarget = Ball.Location.Flatten() + (bot.TheirGoal.Location - Ball.Location.Flatten()).Normalize() * 40 + Vec3.Up * 100;
			bot.Renderer.Line3D(DribbleTarget, DribbleTarget + Vec3.Up * 200, System.Drawing.Color.DarkOliveGreen);
			bot.Renderer.Line3D(Ball.Location, Ball.Location + Vec3.Up * 200, System.Drawing.Color.LimeGreen);
			bot.Renderer.Line3D(Ball.Location, Ball.Location + ballToTarget, System.Drawing.Color.Yellow);

			toTargetDot = bot.Me.Forward.Dot(bot.Me.Location.Flatten().Direction(DribbleTarget));
			toTargetDotRight = bot.Me.Right.Dot(bot.Me.Location.Direction(DribbleTarget));
			bot.Renderer.Text3D($"Preparing the dribble...", bot.Me.Location + new Vec3(0, 0, 100), 2, System.Drawing.Color.OrangeRed);

			if (carToTargetVec.Length() > 800)
			{
				ArriveAction.Target = DribbleTarget;
				ArriveAction.Run(bot);
				return;
			}

			bot.Throttle(Utils.Cap(100 + carToTargetVec.Length() * 12f, 1, Car.MaxSpeed), MathF.Sign(toTargetDot) >= 0 ? false : true);
			bot.Controller.Steer = Utils.Cap(toTargetDotRight * 2, -1, 1);
		}

		// Adjust cause bot drives sluggishly rn
		private void CarryToLocation(Vec3 targetLocation, RUBot bot)
		{
			bot.Renderer.Line3D(targetLocation, targetLocation + Vec3.Up * (100), System.Drawing.Color.Red);
			bot.Renderer.Line3D(bot.Me.Location, targetLocation, System.Drawing.Color.OrangeRed);
			BallSlice targetSlice = PredictDribbleSlice(120f);

			Vec3 offset = (targetLocation - Ball.Location).Flatten();
			offset.y *= 1.8f; // 1.8f;
			offset = offset.FlatNorm();
			offset *= Utils.Cap(bot.Me.Boost, 40, 60);
			offset.y *= Utils.Cap(bot.Me.Velocity.Length() / 1000, 1, 2);
			offset.x *= Utils.Cap(bot.Me.Velocity.Length() / 1000, 1, 1.5f);
			offset.y += -Utils.Cap(dribbleSpeedUp / (2f), 0, 70);
			CarryWithOffset(offset, bot);

		}
		private void CarryWithOffset(Vec3 offset, RUBot bot)
		{
			BallSlice targetSlice = PredictDribbleSlice(120f);

			Vec3 driveTarget = targetSlice.Location - offset;

			Vec3 toTargetFlat = driveTarget.Flatten() - bot.Me.Location.Flatten();
			float speed = bot.Me.Velocity.Dot(toTargetFlat.Normalize());
			float toTargetDot = bot.Me.Forward.Dot(toTargetFlat.Normalize());

			float eta = (toTargetFlat.Length() / speed);

			// targetSlice.Time does not incorporate the offset to the time, might cause unwanted behaviors, look here for bugfixes
			float targetSpeed = bot.Me.Location.FlatDist(driveTarget) / MathF.Max(0.0001f, targetSlice.Time - (Game.Time + eta));
			
			// Draw target Slice
			bot.Renderer.Circle(targetSlice.Location, Vec3.X, Ball.Radius, System.Drawing.Color.Green);
			bot.Renderer.Circle(targetSlice.Location, Vec3.Y, Ball.Radius, System.Drawing.Color.Green);
			bot.Renderer.Circle(targetSlice.Location, Vec3.Z, Ball.Radius, System.Drawing.Color.Green);

			// Draw target ball pos with offset
			bot.Renderer.Circle(driveTarget, Vec3.X, Ball.Radius + 5, System.Drawing.Color.Yellow);
			bot.Renderer.Circle(driveTarget, Vec3.Y, Ball.Radius + 5, System.Drawing.Color.Yellow);
			bot.Renderer.Circle(driveTarget, Vec3.Z, Ball.Radius + 5, System.Drawing.Color.Yellow);

			targetSpeed = MathF.Abs(targetSpeed) * (toTargetDot < -0.1 ? -1 : 1);

			//Speed controller
			Drive(bot, driveTarget, targetSpeed);

			bot.Renderer.Text2D($"targetVel: {MathF.Round(targetSpeed)}, Vel: {MathF.Round(bot.Me.Velocity.Dot(bot.Me.Forward))}, dot: {MathF.Round(toTargetDot, 1)}", new Vec3(0, 50), 1.5f, System.Drawing.Color.White);

		}
		private void Drive(RUBot bot, Vec3 target, float targetSpeed)
		{
			// Debug
			bot.Renderer.Line3D(target.Flatten(), target.Flatten() + Vec3.Up * (50 + waitTimer), System.Drawing.Color.Lime);

			Vec3 toTargetFlat = target.Flatten() - bot.Me.Location.Flatten();
			float speed = bot.Me.Velocity.Dot(toTargetFlat.Normalize());
			float forwardDot = (bot.Me.Forward).Dot(toTargetFlat.Normalize());

			// Steering
			float steeringDir = bot.Me.Right.Dot(bot.Me.Location.Direction(target.Flatten()));
			float steerFactor = 4f;

			bot.Controller.Steer = Utils.Cap(steerFactor * steeringDir, -1, 1);

			bot.Controller.Steer = targetSpeed >= 0 ? bot.Controller.Steer : bot.Controller.Steer * -1;
			bot.Controller.Handbrake = speed > 100 && MathF.Abs(steeringDir) > 0.7f && forwardDot < 0.8f;

			if (targetSpeed < 0f)
			{
				bot.Controller.Throttle = -1;
			}
			else if (speed < targetSpeed)
			{
				bot.Controller.Throttle = 1f;
				if (targetSpeed > 1400 && speed < 2200 && bot.Me.Location.FlatDist(target) > 300)
				{
					bot.Controller.Boost = true;
				}
				else
				{
					bot.Controller.Boost = false;
				}
			}
			else
			{
				if (speed - targetSpeed > 400)
				{
					bot.Controller.Throttle = -1f;
				}
				else if (speed - targetSpeed > 100)
				{
					if (bot.Me.Up.z > 0.85f)
					{
						bot.Controller.Throttle = 0f;
					}
					else
					{
						bot.Controller.Throttle = 0.1f;
					}
				}
				bot.Controller.Boost = false;
			}

			if (forwardDot < 0.3)
			{
				bot.Controller.Boost = false;
			}
		}
		private void DriveToTargetNew(Vec3 target, RUBot bot)
		{
			float steeringDir = bot.Me.Right.Dot(bot.Me.Location.Direction(target));
			float targetSpeed = Car.MaxSpeed;
			float speed = bot.Me.Velocity.Length();

			Vec3 toTarget = target - bot.Me.Location;

			if(targetSpeed > speed)
			{
				bot.Controller.Throttle = 1f;
			}
			else
			{
				if(speed - targetSpeed > 400f)
				{
					bot.Controller.Throttle = -1f;
				}
				else
				{
					bot.Controller.Throttle = -0.5f;
				}
			}
			bot.Controller.Steer = Utils.Cap(3 * steeringDir, -1, 1);
		}
		private void SetCaVel(Vec3 position, RUBot bot)
		{
			Dictionary<int, DesiredCarStateT> carStates = new();
			carStates.Add(0, new()
			{
				Physics = new()
				{
					Velocity = new()
					{
						X = new() { Val = position.x },
						Y = new() { Val = position.y },
						Z = new() { Val = position.z },
					},
				},
			});
			bot.SetGameState(null, carStates);
		}
		private void SetBallPos(Vec3 position, RUBot bot)
		{
			Dictionary<int, DesiredBallStateT> ballStates = new();
			ballStates.Add(0, new()
			{
				Physics = new()
				{
					Location = new()
					{
						X = new() { Val = position.x },
						Y = new() { Val = position.y },
						Z = new() { Val = position.z },
					},
				},
			});
			bot.SetGameState(ballStates);
		}
		private void SetBallVel(Vec3 velocity, RUBot bot)
		{
			Dictionary<int, DesiredBallStateT> ballStates = new();
			ballStates.Add(0, new()
			{
				Physics = new()
				{
					Velocity = new()
					{
						X = new() { Val = velocity.x },
						Y = new() { Val = velocity.y },
						Z = new() { Val = velocity.z },
					},
				},
			});
			bot.SetGameState(ballStates);
		}
		private void SetBall(Vec3 position, Vec3 velocity, RUBot bot)
		{
			Dictionary<int, DesiredBallStateT> ballStates = new();
			ballStates.Add(0, new()
			{
				Physics = new()
				{
					Location = new()
					{
						X = new() { Val = position.x },
						Y = new() { Val = position.y },
						Z = new() { Val = position.z },
					},
					Velocity = new()
					{
						X = new() { Val = velocity.x },
						Y = new() { Val = velocity.y },
						Z = new() { Val = velocity.z },
					},
				},
			});
			bot.SetGameState(ballStates);
		}
		private Vec3 PredictDribbleTarget(float minValueZ)
		{
			BallSlice[] slices = Ball.Prediction.Slices;
			BallSlice soonestSlice = null;
			Vec3 soonestSlicePos = Vec3.Zero;
			foreach(BallSlice slice in slices)
			{
				if(slice.Location.z < minValueZ)
				{
					soonestSlice = slice;
					predBallTime = slice.Time;
					return soonestSlice.Location;
				}
			}
			return Vec3.Zero;
		}
		private BallSlice PredictDribbleSlice(float minValueZ)
		{
			BallSlice[] slices = Ball.Prediction.Slices;
			BallSlice soonestSlice = null;
			Vec3 soonestSlicePos = Vec3.Zero;
			foreach (BallSlice slice in slices)
			{
				if (slice.Location.z < minValueZ)
				{
					soonestSlice = slice;
					predBallTime = slice.Time;
					return soonestSlice;
				}
			}
			return slices[0];
		}
		private void GptDriveToTarget(Vec3 target, RUBot bot)
		{
			Vec3 toTarget = target - bot.Me.Location;
			Vec3 dir = toTarget.Normalize();

			float distance = toTarget.Length();

			float accel = 2000f;
			float maxSpeed = 1400f;

			float targetSpeed = MathF.Sqrt(2 * accel * distance);
			targetSpeed = MathF.Min(targetSpeed, maxSpeed);

			float currentSpeed = bot.Me.Velocity.Dot(dir);

			float speedError = targetSpeed - currentSpeed;

			float throttle = Utils.Cap(speedError / 1000f, -1, 1);

			if (toTarget.Length() < 50)
			{
				throttle = 0;
			}

			bot.Controller.Throttle = throttle;
			//bot.Controller.Brake = throttle < 0;

			float steeringDir = bot.Me.Right.Dot(dir);
			bot.Controller.Steer = Utils.Cap(3 * steeringDir, -1, 1);
		}

		private void MoveTargetInCircle(float rotationSpeed, float rotationDistance)
		{
			float rotationTime = (rotationSpeed * Game.Time);
			Vec3 targetPos = OriginalTarget;
			if (successfulArrives % 3 == 0)
			{
				DribbleTarget = targetPos + new Vec3(MathF.Sin(rotationTime) * rotationDistance, MathF.Cos(rotationTime) * rotationDistance);
			}
			else
			{
				DribbleTarget = OriginalTarget;
			}
		}
	}
}
