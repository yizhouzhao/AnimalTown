#!/bin/bash

export PYTHONPATH=$PYTHONPATH:$pwd

trials=2

# Run ToM agent
{
for i in $(seq 1 $trials)
do
	echo $i
	sleep 2
	python tom/experiment.py --method ToM &
	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
done
}

 # Run ToM gt agent
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python tom/experiment.py &
 	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 }

 # Run chasing agent
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python tom/experiment.py --method chase &
 	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 }

 # Run Passer
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python maddpg/experiments/passer.py &
 	if (( $i % 10 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 }

 # Run MADDPG
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python maddpg/experiments/train_maddpg_for_tom.py &
 	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 } &

# Run ToM gt agent
{
for i in $(seq 1 $trials)
do
	echo $i
	sleep 2
	python tom/experiment.py &
	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
done
}

 # Run chasing agent
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python tom/experiment.py --method chase &
 	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 }

 # Run Passer
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python maddpg/experiments/passer.py &
 	if (( $i % 10 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 }

 # Run MADDPG
 {
 for i in $(seq 1 $trials)
 do
 	echo $i
 	sleep 2
 	python maddpg/experiments/train_maddpg_for_tom.py &
 	if (( $i % 5 == 0 )); then wait; fi # Limit to 10 concurrent subshells.
 done
 } &
wait

python tom/plot_benchmark.py