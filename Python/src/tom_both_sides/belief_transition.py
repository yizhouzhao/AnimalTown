"""
Created on Aug 20, 2018

@author: Siyuan Qi

Description of the file.

"""


import numpy as np

import torch
import torch.nn
import torch.optim
from torch.nn import functional as F


class TransitionKernel(torch.nn.Module):
    def __init__(self, obs_shape, action_shape, belief_size, half_fc_size=32, fc_layer_num=3):
        super(TransitionKernel, self).__init__()

        self.fc_size = half_fc_size * 2
        self.belief_size = belief_size
        self.obs_fc = torch.nn.Linear(obs_shape, half_fc_size)
        self.action_fc = torch.nn.Linear(action_shape, half_fc_size)
        self.fc_layers = torch.nn.ModuleList([torch.nn.Linear(self.fc_size, self.fc_size) for _ in range(fc_layer_num)])
        # Each kernel fc outputs a column for the transition kernel K: b_t = K * b_t-1
        self.kernel_fcs = torch.nn.ModuleList([torch.nn.Linear(self.fc_size, self.belief_size) for _ in range(self.belief_size)])

    def forward(self, obs, action):
        x = torch.cat([self.obs_fc(obs), self.action_fc(action)], dim=-1)
        for i in range(len(self.fc_layers)):
            x = self.fc_layers[i](F.relu(x))

        kernel = []
        for i in range(len(self.kernel_fcs)):
            foo = F.softmax(self.kernel_fcs[i](x), dim=-1).unsqueeze(1)
            kernel.append(foo)
        return torch.cat(kernel, dim=-2)  # Return the transpose of kernel


class BeliefEstimation(torch.nn.Module):
    def __init__(self, obs_shape, belief_size, fc_size=32, fc_layer_num=3):
        super(BeliefEstimation, self).__init__()
        self.obs_fc = torch.nn.Linear(obs_shape, fc_size)
        self.fc_layers = torch.nn.ModuleList([torch.nn.Linear(fc_size, fc_size) for _ in range(fc_layer_num)])
        self.belief_fc = torch.nn.Linear(fc_size, belief_size)

    def forward(self, obs):
        x = self.obs_fc(obs)
        for i in range(len(self.fc_layers)):
            x = self.fc_layers[i](F.relu(x))
        x = self.belief_fc(x)
        return F.softmax(x, dim=-1)


def test():
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    torch.manual_seed(0)

    action = torch.Tensor([1, 0]).to(device)
    obs_t0 = torch.Tensor([[1, 4], [2, 5]]).flatten().to(device)
    obs_t1 = torch.Tensor([[1, 4], [2, 5]]).flatten().to(device)

    b_t0 = torch.Tensor([0.2, 0.8]).to(device)
    b_t1 = torch.Tensor([0.4, 0.6]).to(device)

    kernel_model = TransitionKernel(obs_t0.shape[0], action.shape[0], b_t0.shape[0]).to(device)
    kernel_optimizer = torch.optim.Adam(kernel_model.parameters(), lr=1e-3)
    estimator_model = BeliefEstimation(obs_t0.shape[0], b_t0.shape[0]).to(device)
    estimator_optimizer = torch.optim.Adam(estimator_model.parameters(), lr=1e-3)
    loss_fn = torch.nn.MSELoss()

    for i in range(100):
        kernel = kernel_model.forward(obs_t0, action)
        b_t1_kernel_pred = torch.mm(b_t0.view(1, -1), kernel)
        kernel_loss = loss_fn(torch.squeeze(b_t1_kernel_pred), b_t1)

        kernel_optimizer.zero_grad()
        kernel_loss.backward()
        kernel_optimizer.step()

        b_t1_estimator_pred = estimator_model.forward(obs_t1)
        estimator_loss = loss_fn(b_t1_estimator_pred, b_t1)
        estimator_optimizer.zero_grad()
        estimator_loss.backward()
        estimator_optimizer.step()

    # Print final result
    kernel = kernel_model.forward(obs_t0, action)
    b_t1_kernel_pred = torch.mm(b_t0.view(1, -1), kernel)
    print('Transition kernel:', kernel, b_t1_kernel_pred)
    b_t1_estimator_pred = estimator_model.forward(obs_t1)
    print('Observation estimation:', b_t1_estimator_pred)


def main():
    test()


if __name__ == '__main__':
    main()
