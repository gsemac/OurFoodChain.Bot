local p = {}

-- string parsing functions

function p.trim(frame)
    return trim(frame.args[1])
end

function trim(input) 
    return input:gsub("^%s+", ""):gsub("%s+$", "")
end

function split(input, delimiter)
    
    if delimiter == nil then
        delimiter = " "
    end

    local pattern = string.format("([^%s]+)", delimiter)
    local result = {}

    for substr in input:gmatch(pattern) do
        result[#result + 1] = substr
    end

    return result

end

-- string formatting functions

function p.splitToFormat(frame) 
    return splitToFormat(frame.args[1], frame.args[2], frame.args[3])
end

function splitToFormat(input, delimiter, format)

    local arr = split(input, delimiter);
    local result = "";

    for i = 1, #arr do
        result = result .. string.format(format, trim(arr[i]))
    end

    return result;

end

-- category functions

function p.splitToCategoryTags(frame) 
    return splitToCategoryTags(frame.args[1], frame.args[2])
end

function splitToCategoryTags(input, delimiter) 
    return splitToFormat(input, delimiter, "[[Category:%s]]\n")
end

return p