function OnRegister(move)

	move.SetName("Bright Glow")
	move.SetDescription("Glows brightly, reducing the opponent's accuracy.")

	move.SetPP(10)

	move.Requires.DescriptionMatch("glow|\blight\b|bioluminescen(?:t|ce)")

end

function OnMove(args) 	
	
	amount = 0.05

	args.Target.Stats.Acc = args.Target.Stats.Acc * (1.0 - amount)

end