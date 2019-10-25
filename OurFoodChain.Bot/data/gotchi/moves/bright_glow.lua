function OnRegister(move)

	move.SetName("Bright Glow")
	move.SetDescription("Glows brightly, reducing the opponent's accuracy.")

	move.SetPP(10)

	move.Requires.DescriptionMatch("glow|\blight\b|bioluminescen(?:t|ce)")

end

function OnMove(args) 	

	args.Target.Stats.DebuffPercent("acc", 5)

end