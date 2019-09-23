function OnRegister(move)

	move.SetName("Take Root")
	move.SetDescription("Takes root and draws nutrients from the substrate, restoring HP each turn.")
	move.SetType("producer")

	move.SetPP(10)

	move.Requires.TypeMatch("producer").MinimumLevel(30).DescriptionMatch("root")

end

function OnMove(args) 	
	
	args.SetText("digging its roots into the ground")

	args.User.SetStatus("rooted")

end