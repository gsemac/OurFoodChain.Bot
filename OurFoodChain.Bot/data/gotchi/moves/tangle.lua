function OnRegister(move)

	move.SetName("tangle")
	move.SetDescription("Tangles the opponent in vines, lowering their speed.")
	move.SetType("producer")

	move.SetPP(10)

	move.Requires.TypeMatch("producer").DescriptionMatch("vine")

end

function OnMove(args) 	
	
	args.Target.Stats.DebuffPercent("spd", 20)

	args.SetText("lowering the opponent's speed by 20%")

end