function [ status ] = WriteMatToFile( inMat, fileName )
%WriteMatToFile Summary of this function goes here
%   Detailed explanation goes here
%   Writes a Matrix to binary file to be read from a c++ program

fileOut = fopen(fileName,'w+');
% fileOut = fopen(fileName,'W');
fwrite(fileOut, size(inMat,1), 'double');
fwrite(fileOut, size(inMat,2), 'double');
fwrite(fileOut, inMat, 'double');

status = fclose(fileOut);
end

