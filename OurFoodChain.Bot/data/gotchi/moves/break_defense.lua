function OnRegister(move)

	move.SetName("break defense")
	move.SetDescription("Breaks down the opponent's defense, allowing them to go all-out. Reducing the opponent's Defense to 0, but ups their Attack.")
	move.SetType("decomposer")

	move.SetPP(5)
	
	move.Requires.TypeMatch("decomposer")
	move.Requires.MinimumLevel(30)

end

function OnMove(args) 

	args.Target.Stats.Atk = args.Target.Stats.Atk + args.Target.Stats.Def
	args.Target.Stats.Def = 0

	args.SetText("breaking the opponent's defense")

end