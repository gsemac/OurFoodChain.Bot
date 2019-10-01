function OnRegister(move)

	move.SetName("molt")
	move.SetDescription("Molts exoskeleton, undoing all stat changes and restoring HP.")

	move.SetPP(5)

	move.Requires.DescriptionMatch("molt")

end

function OnMove(args) 

	args.User.ResetStats()
	args.User.ClearStatus()

	args.RecoverPercent(20)

	args.SetText("resetting all stat changes and restoring HP")

end