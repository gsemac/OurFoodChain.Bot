function OnRegister(move)

	move.SetName("helpless facade")
	move.SetDescription("The user appears helpless and harmless, causing the opponent to let their guard down, lowering their DEF.")

	move.SetPP(5)
	move.SetIgnoreAccuracy(true)

	move.Requires.DescriptionMatch("blind")

end

function OnMove(args) 

	args.Target.Stats.DebuffPercent("def", 20)

end