import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { 
  Container, Box, Typography, Button, Stepper, 
  Step, StepLabel, Paper, Divider, 
  List, ListItem, ListItemText, Checkbox,
  Alert, CircularProgress 
} from '@mui/material';
import { ArrowForward, ArrowBack, CheckCircle, Warning } from '@mui/icons-material';

const API_BASE_URL = 'http://localhost:5204/api';

const DocumentWorkflow = ({ documentId }) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [workflowData, setWorkflowData] = useState(null);
  const [statuses, setStatuses] = useState([]);
  const [movingNext, setMovingNext] = useState(false);
  const [movingBack, setMovingBack] = useState(false);
  const [comments, setComments] = useState('');

  // Fetch workflow data
  const fetchWorkflowNavigation = async () => {
    try {
      setLoading(true);
      const response = await axios.get(`${API_BASE_URL}/Workflow/document/${documentId}/workflow-navigation`);
      setWorkflowData(response.data);
      await fetchStepStatuses();
    } catch (err) {
      setError(err.response?.data || 'Failed to load workflow data');
    } finally {
      setLoading(false);
    }
  };

  // Fetch statuses for the current step
  const fetchStepStatuses = async () => {
    try {
      const response = await axios.get(`${API_BASE_URL}/Workflow/document/${documentId}/step-statuses`);
      setStatuses(response.data);
    } catch (err) {
      console.error('Failed to load statuses:', err);
    }
  };

  // Initialize component
  useEffect(() => {
    if (documentId) {
      fetchWorkflowNavigation();
    }
  }, [documentId]);

  // Handle status toggle
  const handleStatusToggle = async (statusId, isComplete) => {
    try {
      await axios.post(`${API_BASE_URL}/Workflow/complete-status`, {
        documentId,
        statusId,
        isComplete: !isComplete,
        comments: `Status ${!isComplete ? 'completed' : 'uncompleted'}`
      });
      
      await fetchStepStatuses();
      await fetchWorkflowNavigation(); // Refresh navigation info (can move next?)
    } catch (err) {
      setError(err.response?.data || 'Failed to update status');
    }
  };

  // Move to next step
  const moveToNextStep = async () => {
    try {
      setMovingNext(true);
      await axios.post(`${API_BASE_URL}/Workflow/move-next`, {
        documentId,
        comments: comments || 'Moving to next step'
      });
      
      setComments('');
      await fetchWorkflowNavigation();
    } catch (err) {
      setError(err.response?.data || 'Failed to move to next step');
    } finally {
      setMovingNext(false);
    }
  };

  // Return to previous step
  const returnToPreviousStep = async () => {
    try {
      setMovingBack(true);
      await axios.post(`${API_BASE_URL}/Workflow/return-to-previous`, {
        documentId,
        comments: comments || 'Returning to previous step'
      });
      
      setComments('');
      await fetchWorkflowNavigation();
    } catch (err) {
      setError(err.response?.data || 'Failed to return to previous step');
    } finally {
      setMovingBack(false);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" my={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        {error}
      </Alert>
    );
  }

  if (!workflowData) {
    return (
      <Alert severity="info" sx={{ mt: 2 }}>
        No workflow data available for this document.
      </Alert>
    );
  }

  return (
    <Container maxWidth="md">
      <Paper elevation={3} sx={{ p: 3, mt: 3 }}>
        <Typography variant="h5" gutterBottom>
          Document Workflow
        </Typography>
        
        <Typography variant="subtitle1" color="text.secondary">
          {workflowData.circuitTitle} | Current step: {workflowData.currentStepTitle}
        </Typography>

        {/* Workflow Stepper */}
        <Box sx={{ my: 4 }}>
          <Stepper activeStep={workflowData.steps.findIndex(s => s.isCurrent)} alternativeLabel>
            {workflowData.steps.map((step) => (
              <Step key={step.stepId}>
                <StepLabel>{step.title}</StepLabel>
              </Step>
            ))}
          </Stepper>
        </Box>

        <Divider sx={{ my: 2 }} />
        
        {/* Current Step Statuses */}
        <Typography variant="h6" gutterBottom>
          Required Tasks
        </Typography>
        
        <List>
          {statuses.map((status) => (
            <ListItem key={status.statusId}>
              <Checkbox
                edge="start"
                checked={status.isComplete}
                onChange={() => handleStatusToggle(status.statusId, status.isComplete)}
                disabled={workflowData.isComplete}
              />
              <ListItemText 
                primary={status.title} 
                secondary={status.isRequired ? 'Required' : 'Optional'}
              />
              {status.isComplete && status.completedBy && (
                <Typography variant="caption" color="text.secondary">
                  Completed by {status.completedBy}
                </Typography>
              )}
            </ListItem>
          ))}
        </List>

        <Divider sx={{ my: 2 }} />
        
        {/* Navigation Controls */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 3 }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            onClick={returnToPreviousStep}
            disabled={!workflowData.canReturnToPreviousStep || movingBack || workflowData.isComplete}
          >
            {movingBack ? <CircularProgress size={24} /> : 'Previous Step'}
          </Button>
          
          {workflowData.isComplete ? (
            <Alert icon={<CheckCircle />} severity="success">
              Workflow Complete
            </Alert>
          ) : !workflowData.canMoveToNextStep ? (
            <Alert icon={<Warning />} severity="info">
              Complete all required tasks to proceed
            </Alert>
          ) : null}
          
          <Button
            variant="contained"
            endIcon={<ArrowForward />}
            onClick={moveToNextStep}
            disabled={!workflowData.canMoveToNextStep || movingNext || workflowData.isComplete}
            color="primary"
          >
            {movingNext ? <CircularProgress size={24} color="inherit" /> : 'Next Step'}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default DocumentWorkflow; 