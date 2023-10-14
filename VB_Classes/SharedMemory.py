#coding: utf-8
 # ----------------------------------------------------------------------------
 # ****************************************************************************
 # * @file SharedMemory.py
 # * @author Isaac Jesus da Silva - ROBOFEI-HT - FEI 😛
 # * @version V0.0.2
 # * @created 08/09/2015
 # * @Modified 16/09/2015
 # * @e-mail isaac25silva@yahoo.com.br
 # * @brief Shared Memory 😛
 # ****************************************************************************
 # ****************************************************************************
import ctypes
import time
import os

# Classe do BlackBoard--------------------------------------------------------------------
class SharedMemory(object):
	''' Classe que lê e escreve na memória compartilhada do sistema '''

	def __init__(self):
		print("Start the Class Blackboard")
		# Usando memoria compartilhada a partir das funções do c++-------------------------------------------------------
		self.testlib = ctypes.CDLL('./libblackboardpy.so') #chama a lybrary que contem as funções em c++
		self.testlib.using_shared_memory()         #using c++ function
		self.testlib.leitura_float.restype = ctypes.c_float #defining the return type, that case defining float
		self.testlib.leitura_int.restype = ctypes.c_int #defining the return type, that case defining int
		#--------------------------------------------------------------------------------------------------------------------

	# Criando função que escreve float--------------------------------------------------------
	def write_float(self, variable, value):
		self.testlib.escreve_float(self.variable_float[variable], ctypes.c_float(value))
	#-----------------------------------------------------------------------------------------

	# Criando função que escreve float--------------------------------------------------------
	def write_int(self, variable, value):
		self.testlib.escreve_int(self.variable_int[variable], ctypes.c_int(int(value)))
	#-----------------------------------------------------------------------------------------

	# Criando função que escreve float--------------------------------------------------------
	def read_float(self, variable):
		return self.testlib.leitura_float(self.variable_float[variable])
	#-----------------------------------------------------------------------------------------

	# Criando função que escreve float--------------------------------------------------------
	def read_int(self, variable):
		return self.testlib.leitura_int(self.variable_int[variable])
	#-----------------------------------------------------------------------------------------

	variable_int = {
	"PLANNING_COMMAND" : 0,
	"PLANNING_PARAMETER_VEL": 1,
	"PLANNING_PARAMETER_ANGLE": 2,
	"IMU_STATE" : 3,
	"CONTROL_ACTION": 13,
	"CONTROL_HEIGHT_A": 14,
	"CONTROL_HEIGHT_B": 15,
	"CONTROL_HEIGHT_C": 16,
	"DECISION_ACTION_A": 17,
	"DECISION_ACTION_B": 18,
	"DECISION_STATE": 19,
	"DECISION_POSITION_A": 20,
	"DECISION_POSITION_B": 21,
	"DECISION_POSITION_C": 22,
	"DECISION_BALL_POS": 23,
	"DECISION_OPP1_POS": 24,
	"DECISION_OPP2_POS": 25,
	"DECISION_OPP3_POS": 26,
	"COM_ACTION_ROBOT1": 27,
	"COM_ACTION_ROBOT2": 28,
	"COM_ACTION_ROBOT3": 29,
	"COM_STATE_ROBOT1": 30,
	"COM_STATE_ROBOT2": 31,
	"COM_STATE_ROBOT3": 32,
	"COM_POS_ROBOT1": 33,
	"COM_POS_ROBOT2": 34,
	"COM_POS_ROBOT3": 35,
	"COM_POS_BALL_ROBOT1": 36,
	"COM_POS_BALL_ROBOT2": 37,
	"COM_POS_BALL_ROBOT3": 38,
	"COM_POS_OPP_A_ROBOT1": 39,
	"COM_POS_OPP_A_ROBOT2": 40,
	"COM_POS_OPP_A_ROBOT3": 41,
	"COM_POS_OPP_A_ROBOT4": 42,
	"COM_POS_OPP_B_ROBOT1": 43,
	"COM_POS_OPP_B_ROBOT2": 44,
	"COM_POS_OPP_B_ROBOT3": 45,
	"COM_POS_OPP_B_ROBOT4": 46,
	"COM_POS_OPP_C_ROBOT1": 47,
	"COM_POS_OPP_C_ROBOT2": 48,
	"COM_POS_OPP_C_ROBOT3": 49,
	"COM_POS_OPP_C_ROBOT4": 50,
	"COM_REFEREE": 51,
	"LOCALIZATION_X": 52,
	"LOCALIZATION_Y": 53,
	"LOCALIZATION_THETA": 54,
	"VISION_MOTOR1_ANGLE": 55,
	"VISION_MOTOR2_ANGLE": 56,
	"VISION_LOST_BALL": 57,
	"VISION_SEARCH_BALL": 58,
	"DECISION_ACTION_VISION": 59,
	"VISION_MOTOR1_GOAL": 60,
	"VISION_MOTOR2_GOAL": 61,
	"VISION_SEARCH_GOAL": 62,
	"VISION_LOST_GOAL": 63,
	"VISION_STATE": 64,
	"ROBOT_NUMBER": 65,
	}

	variable_float = {
	"VISION_DIST_BALL": 1,
	"VISION_DIST_GOAL": 2,
	"VISION_DIST_OPP1": 3,
	"VISION_DIST_OPP2": 4,
	"VISION_DIST_OPP3": 5,
	"IMU_GYRO_X": 6,
	"IMU_GYRO_Y": 7,
	"IMU_GYRO_Z": 8,
	"IMU_ACCEL_X": 9,
	"IMU_ACCEL_Y": 10,
	"IMU_ACCEL_Z": 11,
	"IMU_COMPASS_X": 12,
	"IMU_COMPASS_Y": 13,
	"IMU_COMPASS_Z": 14,
	"IMU_EULER_X": 15,
	"IMU_EULER_Y": 16,
	"IMU_EULER_Z": 17,
	"IMU_QUAT_X": 18,
	"IMU_QUAT_Y": 19,
	"IMU_QUAT_Z": 20,
	}
#------------------------------------------------------------------------------------------