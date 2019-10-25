function OnRegister(move)

	move.SetName("cast shade")
	move.SetDescription("Casts shade over the opponent, preventing them from using Grow or Photosynthesis.")
	move.SetType("producer")

	move.SetPP(25)
	move.SetIgnoreCritical(true)

	move.Requires.TypeMatch("producer")
	move.Requires.MinimumLevel(10)
	move.Requires.DescriptionMatch("tree|tall|heavy")

end

function OnMove(args) 	
	
	args.SetText("casting shade on the opponent")
	args.Target.SetStatus("shaded")

end