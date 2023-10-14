Imports cv = OpenCvSharp
'//contrib.hpp

'Class CV_EXPORTS LDA
'    {
'    Public: 
'       //Initializes a LDA with num_components (default 0) And specifies how
'       //samples are aligned (default dataAsRow=true).
'        LDA(int num_components = 0) :
'            _num_components(num_components) {};

'       //Initializes And performs a Discriminant Analysis with Fisher's
'       //Optimization Criterion on given data in src And corresponding labels
'       //in labels. If 0 (Or less) number of components are given, they are
'       //automatically determined for given data in computation.
'        LDA(const Mat& src, vector<int> labels,
'                int num_components = 0) :
'                    _num_components(num_components)
'        {
'            this->compute(src, labels);//! compute eigenvectors And eigenvalues
'        }

'       //Initializes And performs a Discriminant Analysis with Fisher's
'       //Optimization Criterion on given data in src And corresponding labels
'       //in labels. If 0 (Or less) number of components are given, they are
'       //automatically determined for given data in computation.
'        LDA(InputArrayOfArrays src, InputArray labels,
'                int num_components = 0) :
'                    _num_components(num_components)
'        {
'            this->compute(src, labels);//! compute eigenvectors And eigenvalues
'        }

'       //Serializes this object to a given filename.
'        void save(const string& filename) Const;

'       //Deserializes this object from a given filename.
'        void load(const string& filename);

'       //Serializes this object to a given cvFileStorage.
'        void save(FileStorage& fs) const;

'           //Deserializes this object from a given cvFileStorage.
'        void load(const FileStorage& node);

'       //Destructor.
'        ~LDA() {}

'       //! Compute the discriminants for data in src And labels.
'        void compute(InputArrayOfArrays src, InputArray labels);

'       //Projects samples into the LDA subspace.
'        Mat project(InputArray src);

'       //Reconstructs projections from the LDA subspace.
'        Mat reconstruct(InputArray src);

'       //Returns the eigenvectors of this LDA.
'        Mat eigenvectors() Const { Return _eigenvectors; };

'       //Returns the eigenvalues of this LDA.
'        Mat eigenvalues() Const { Return _eigenvalues; }

'    Protected:
'        bool _dataAsRow;
'        int _num_components;
'        Mat _eigenvectors;
'        Mat _eigenvalues;

'        void lda(InputArrayOfArrays src, InputArray labels);
'    };
'2. Demo sample

'//LDA.cpp: defines the entry point Of the console application.
'//

'#include "stdafx.h"
'#include <iostream>
'#include <contrib\contrib.hpp>
'#include <cxcore.hpp>
'Using namespace cv;
'Using namespace std;

'int main(Void)
'{
'	//sampledata
'	Double sampledata[6][2]={ {0, 1}, {0, 2}, {2, 4}, {8, 0}, {8, 2}, {9, 4}};
'	Mat mat = mat(6, 2, CV_64FC1, sampledata);
'	//labels
'	vector<int>labels;
'	For (int i= 0;i<mat.rows;i++)
'	{
'		If (i < mat.rows / 2)
'		{
'			labels.push_back(0);
'		}
'		Else
'		{
'			labels.push_back(1);
'		}
'	}

'	//do LDA
'	LDA lda = lda(mat, labels);
'	//get the eigenvector
'	Mat eivector = lda.eigenvectors().clone();

'	cout<<"The eigenvector is:"<<endl;
'	For (int i= 0;i<eivector.rows;i++)
'	{
'		For (int j= 0;j<eivector.cols;j++)
'		{
'			cout<<eivector.ptr<double>(i)[j]<<" ";
'		}
'		cout<<endl;
'	}

'	//For two types of classification problems, calculate the center of the two data sets
'	int classNum = 2;
'	vector<Mat> classmean(classNum);
'	vector<int> setNum(classNum);

'	For (int i= 0;i<classNum;i++)
'	{
'		classmean[i] = mat : zeros(1,mat.cols,mat.type());
'		setNum[i] = 0;
'	}

'	Mat instance;
'	For (int i= 0;i<mat.rows;i++)
'	{
'		instance=mat.row(i);
'		If (labels[i]==0)
'		{	
'			add(classmean[0], instance, classmean[0]);
'			setNum[0]++;
'		}
'		ElseIf (labels[i]==1)
'		{
'			add(classmean[1], instance, classmean[1]);
'			setNum[1]++;
'		}
'		Else
'		{}
'	}
'	For (int i= 0;i<classNum;i++)
'	{
'		classmean[i].convertTo(classmean[i],CV_64FC1,1.0/static_cast<Double>(setNum[i]));
'	}

'	vector<Mat> cluster(classNum);
'	For (int i= 0;i<classNum;i++)
'	{
'		cluster[i] = mat : zeros(1,1,mat.type());
'		multiply(eivector.t(),classmean[i],cluster[i]);
'	}

'	cout<<"The project cluster center is:"<<endl;
'	For (int i= 0;i<classNum;i++)
'	{
'		cout<<cluster[i].at<double>(0)<<endl;
'	}

'	system("pause");
'	Return 0;
'}


' https://blog.krybot.com/a?ID=01350-140eb99b-7a39-4436-b4b9-c60b059480c8
Public Class LDA_Test : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "", ""}
        desc = "Linear Discriminant Analysis test"
    End Sub
    Public Sub RunVB(src as cv.Mat)
    End Sub
End Class
