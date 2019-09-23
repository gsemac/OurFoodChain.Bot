function OnRegister(move)

	move.SetName("Toxins")
	move.SetDescription("Poisons the opponent, causing them to take damage every turn.")

	move.SetPP(10)

	move.Requires.DescriptionMatch("toxin|poison")

end

function OnMove(args) 
	
	args.Target.SetStatus("poisoned")

	args.SetText("poisoning the opponent")

end