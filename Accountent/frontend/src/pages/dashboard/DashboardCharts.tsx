import { useMemo } from 'react';
import { Card, Col, Empty, Row } from 'antd';
import { Column, Line, Pie } from '@ant-design/plots';
import type { DashboardData } from '../../types/dashboard';

interface Props {
  data: DashboardData;
}

export default function DashboardCharts({ data }: Props) {
  const incomeExpenseData = useMemo(
    () =>
      data.income_expense_chart.flatMap((p) => [
        { period: p.period, type: 'Доходы', value: p.income },
        { period: p.period, type: 'Расходы', value: p.expenses },
      ]),
    [data.income_expense_chart],
  );

  const expensePieData = useMemo(
    () =>
      data.expense_structure.map((e) => ({
        type: `${e.number} ${e.name}`,
        value: e.amount,
      })),
    [data.expense_structure],
  );

  const balanceLineData = useMemo(
    () =>
      data.balance_dynamics.map((b) => ({
        period: b.period,
        account: `${b.account_number}`,
        balance: b.balance,
      })),
    [data.balance_dynamics],
  );

  return (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        <Card title="Доходы и расходы">
          {incomeExpenseData.length > 0 ? (
            <Column
              data={incomeExpenseData}
              xField="period"
              yField="value"
              colorField="type"
              group
              height={280}
              legend={{ position: 'top' }}
            />
          ) : (
            <Empty />
          )}
        </Card>
      </Col>
      <Col xs={24} md={12}>
        <Card title="Структура расходов">
          {expensePieData.length > 0 ? (
            <Pie
              data={expensePieData}
              angleField="value"
              colorField="type"
              radius={0.8}
              height={260}
              label={{ text: 'value', style: { fontSize: 10 } }}
            />
          ) : (
            <Empty />
          )}
        </Card>
      </Col>
      <Col xs={24} md={12}>
        <Card title="Динамика сальдо (50, 51, 60, 62)">
          {balanceLineData.length > 0 ? (
            <Line
              data={balanceLineData}
              xField="period"
              yField="balance"
              colorField="account"
              height={260}
              smooth
            />
          ) : (
            <Empty />
          )}
        </Card>
      </Col>
    </Row>
  );
}
