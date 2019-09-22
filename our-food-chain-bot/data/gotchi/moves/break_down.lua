function OnRegister(move)

	move.SetName("break down")
	move.SetDescription("Resets all of the opponent's stat buffs.")

	move.SetPP(1)

	move.Requires.TypeMatch("decomposer")
	move.Requires.MinimumLevel(20)

end

function OnMove(args)
	
	args.Target.ResetStats()

	args.SetText("resetting its opponent's stats")

end