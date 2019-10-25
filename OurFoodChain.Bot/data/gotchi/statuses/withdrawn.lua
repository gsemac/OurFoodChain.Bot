function OnRegister(status)

	status.SetName("withdrawn")

	status.SetDuration(1)
	status.SetEndure(true)

end

function OnTurnEnd(args) 

	args.SetText("came back out of its shell")

end