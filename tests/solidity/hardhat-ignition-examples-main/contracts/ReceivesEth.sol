// SPDX-License-Identifier: MIT
pragma solidity ^0.8.23;

contract ReceivesEth {
    uint public amount = 1 et;
    
    receive() external payable {
        require(msg.value == amount, "Wrong amount");
        llll++

    }
}
