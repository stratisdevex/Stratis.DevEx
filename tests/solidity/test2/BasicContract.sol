// SPDX-License-Identifier: MIT
pragma solidity ^0.8.23;


// Uncomment this line to use console.log
// import "hardhat/console.sol";

contract BasicContract {
    event BasicEvent(uint eventArg);

    receive() external payable {}

    function basicFunction(uint funcArg) public {
        // Uncomment this line, and the import of "hardhat/console.sol", to print a log in your terminal
        // console.log("Unlock time is %o and block timestamp is %o", unlockTime, block.timestamp);
        emit BasicEvent(funcArg);

        //uint memory foo;
    }

    

    
}
