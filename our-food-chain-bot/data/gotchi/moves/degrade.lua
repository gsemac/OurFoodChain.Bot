function OnRegister(move)

	move.SetName("degrade")
	move.SetDescription("Degrades the opponent, reducing their stats by a small amount.")
	move.SetType("decomposer")

	move.SetPP(10)

	move.Requires.TypeMatch("decomposer")
	move.Requires.MinimumLevel(10)

end

function OnMove(args) 	
	
	args.Target.Stats.BuffPercent(0.8)

	args.SetText("lowering their opponent's stats by 20%")

end