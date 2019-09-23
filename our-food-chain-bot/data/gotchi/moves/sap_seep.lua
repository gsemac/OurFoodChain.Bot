function OnRegister(move)

	move.SetName("Sap Seep")
	move.SetDescription("Seeps sticky sap onto the opponent, decreasing their speed by 30%.")

	move.SetPP(5)

	move.Requires.TypeMatch("producer")
	move.Requires.DescriptionMatch("\\b(?:sap|sticky)\\b")

end

function OnMove(args) 		

	args.Target.Stats.DebuffPercent("spd", 30)

end